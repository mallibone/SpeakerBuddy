namespace StageBuddy.App.EndToEndTests;

/// <summary>
/// Drives the real iOS app on the simulator via the DevFlow agent. Opt-in: STAGEBUDDY_E2E=1.
/// Every test starts from a freshly installed app (clean data container) and only ever
/// navigates forward through in-app buttons — the native nav-bar back chevron is outside
/// the DevFlow agent's reach (see docs/devflow-playbook.md).
/// </summary>
[Collection("Simulator")]
public sealed class SimulatorEndToEndTests(SimulatorFixture fixture)
{
    private DevFlow Ui => fixture.DevFlow;

    private const string SampleTalkTitle = "Building MCP Servers in .NET";
    private const string FirstSnippet = "dotnet add package ModelContextProtocol";
    private const string SecondSnippet = "builder.Services.AddMcpServer()\n    .WithStdioServerTransport();";

    [Fact]
    public void ImportHappyPath_PastedJsonLandsInLibrary()
    {
        E2E.SkipUnlessEnabled();
        fixture.FreshInstall();

        Ui.WaitFor("LibraryEmptyState");
        Ui.Tap("LibraryImportButton");
        Ui.WaitFor("ImportJsonEditor");

        SetImportJson(SimulatorFixture.TestData("sample-talk.json"), marker: "session");
        Ui.Tap("ImportSubmitButton");

        // Success pops back to the library, which reloads and shows the imported talk.
        Ui.WaitForGone("ImportJsonEditor");
        Ui.WaitForText(SampleTalkTitle);
    }

    [Fact]
    public void ImportInvalidJson_ShowsValidationErrorsAndStaysOnPage()
    {
        E2E.SkipUnlessEnabled();
        fixture.FreshInstall();

        Ui.WaitFor("LibraryEmptyState");
        Ui.Tap("LibraryImportButton");
        Ui.WaitFor("ImportJsonEditor");

        SetImportJson("{ \"mangled\": true }", marker: "mangled");
        Ui.Tap("ImportSubmitButton");

        Ui.WaitFor("ImportMessages");
        Assert.True(Ui.Exists("ImportJsonEditor"), "should still be on the import page after a failed import");
    }

    [Fact]
    public void FullLiveRun_InRehearsalMode_CopiesSnippetsAndLandsOnSummary()
    {
        E2E.SkipUnlessEnabled();
        fixture.FreshInstall(("e2e-talk", SimulatorFixture.TestData("sample-talk.json")));

        EnableRehearsalMode();

        Ui.Tap("TalkRow-e2e-talk");
        Ui.WaitFor("LiveStartButton");
        Assert.True(Ui.Exists("RehearsalBadge"), "rehearsal badge should be visible during a rehearsal run");

        Ui.Tap("LiveStartButton");

        // Beat 1 is a talk beat: advance manually.
        Ui.WaitFor("NextBeatButton");
        Ui.Tap("NextBeatButton");

        // Beat 2 is a demo beat: Copy sends to the simulator clipboard and auto-advances.
        Ui.WaitFor("DemoCopyButton");
        Ui.Tap("DemoCopyButton");
        DevFlow.WaitUntil(() => Simulator.Pasteboard() == FirstSnippet, 15, "first snippet did not land on the clipboard");

        // Last step of the last beat: Copy finishes the talk and navigates to the summary.
        Ui.Tap("DemoCopyButton");
        DevFlow.WaitUntil(() => Simulator.Pasteboard() == SecondSnippet, 15, "second snippet did not land on the clipboard");

        Ui.WaitFor("SummaryDoneButton");
        Assert.True(Ui.Exists("SummaryTotalDelta"), "summary should show the total actual-vs-planned delta");

        // Done pops to the library, whose reload picks up the persisted run log.
        Ui.Tap("SummaryDoneButton");
        Ui.WaitFor("TalkRow-e2e-talk");
        Ui.WaitFor("TalkLastRun-e2e-talk");
    }

    [Fact]
    public void CopyWithoutMac_ShowsFailureAndQrFallback()
    {
        E2E.SkipUnlessEnabled();
        fixture.FreshInstall(("e2e-talk", SimulatorFixture.TestData("sample-talk.json")));

        // Rehearsal mode stays OFF: the Multipeer sender has no Mac to talk to on a simulator.
        Ui.WaitFor("TalkRow-e2e-talk");
        Ui.Tap("TalkRow-e2e-talk");
        Ui.WaitFor("LiveStartButton");
        Ui.Tap("LiveStartButton");

        Ui.WaitFor("NextBeatButton");
        Ui.Tap("NextBeatButton");

        // Disconnected sender: the QR fallback renders automatically under the snippet.
        Ui.WaitFor("DemoCopyButton");
        Ui.WaitFor("QrFallbackCaption");

        // Copy must fail visibly and must NOT advance the step.
        Ui.Tap("DemoCopyButton");
        Ui.WaitFor("DemoCopyFailedLabel");
    }

    [Fact]
    public void PlaceholderSheet_ResolvesOncePerRun()
    {
        E2E.SkipUnlessEnabled();
        fixture.FreshInstall(("placeholder-talk", SimulatorFixture.TestData("placeholder-talk.json")));

        EnableRehearsalMode();

        Ui.Tap("TalkRow-placeholder-talk");
        Ui.WaitFor("LiveStartButton");
        Ui.Tap("LiveStartButton");

        // First Copy of a run with placeholders shows the sheet instead of sending.
        Ui.WaitFor("DemoCopyButton");
        Ui.Tap("DemoCopyButton");
        Ui.WaitFor("PlaceholderContinueButton");

        // {{date}} is prefilled from the clock; {{host}} starts empty.
        var today = DateTime.Now.ToString("yyyy-MM-dd");
        Assert.Contains(today, Ui.Query("PlaceholderEntry-date"));

        Assert.True(Ui.TryFill("PlaceholderEntry-host", "demo.local"), "filling the host placeholder entry failed");
        Ui.Tap("PlaceholderContinueButton");

        // Submitting sends immediately with substituted values.
        DevFlow.WaitUntil(
            () => Simulator.Pasteboard() == $"ssh demo.local # provisioned {today}",
            15, "substituted snippet did not land on the clipboard");

        // Second Copy reuses the cached values silently — no sheet, straight to the clipboard.
        Ui.WaitFor("DemoCopyButton");
        Ui.Tap("DemoCopyButton");
        Assert.False(Ui.Exists("PlaceholderContinueButton"), "placeholder sheet must not reappear within the same run");
        DevFlow.WaitUntil(
            () => Simulator.Pasteboard() == "echo demo.local",
            15, "second snippet did not reuse the cached placeholder values");
    }

    /// <summary>Settings round trip: turn rehearsal mode on, return via the in-app Done button.</summary>
    private void EnableRehearsalMode()
    {
        Ui.WaitFor("LibrarySettingsButton");
        Ui.Tap("LibrarySettingsButton");
        Ui.WaitFor("SettingsRehearsalToggleButton");
        Ui.Tap("SettingsRehearsalToggleButton");
        Ui.Tap("SettingsDoneButton");
        Ui.WaitForGone("SettingsDoneButton");
    }

    /// <summary>
    /// Puts JSON into the import editor. Primary path is DevFlow `ui fill`; if the editor
    /// doesn't reflect the text (fill on multiline editors is a preview feature), falls back
    /// to the app's own "Paste from Clipboard" flow via the simulator pasteboard.
    /// </summary>
    private void SetImportJson(string json, string marker)
    {
        if (Ui.TryFill("ImportJsonEditor", json) && Ui.Query("ImportJsonEditor").Contains(marker, StringComparison.Ordinal))
            return;

        Simulator.SetPasteboard(json);
        Ui.Tap("ImportPasteButton");
        DevFlow.WaitUntil(
            () => Ui.Query("ImportJsonEditor").Contains(marker, StringComparison.Ordinal),
            10, "import editor never received the JSON (fill and clipboard-paste both failed)");
    }
}
