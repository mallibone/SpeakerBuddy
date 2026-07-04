namespace StageBuddy.App.EndToEndTests;

/// <summary>
/// Thin wrapper over the `maui devflow` CLI, pinned to the iOS-simulator platform.
/// All element addressing goes through AutomationIds (stable across UI-polish label changes);
/// text addressing is reserved for user content (talk titles etc.) that tests control themselves.
/// </summary>
sealed class DevFlow
{
    private static readonly string MauiTool = ResolveMauiTool();

    private static string ResolveMauiTool()
    {
        var globalTool = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".dotnet", "tools", "maui");
        return File.Exists(globalTool) ? globalTool : "maui";
    }

    private static CliResult Invoke(params string[] args)
        => Cli.Run(MauiTool, ["devflow", "-p", "ios", "--json", .. args], timeout: TimeSpan.FromSeconds(60));

    public void EnsureBrokerRunning()
        => Cli.Run(MauiTool, ["devflow", "broker", "start"], timeout: TimeSpan.FromSeconds(30));

    /// <summary>
    /// True once an iOS agent is registered with the broker. Checking the agent list (not `ui status`)
    /// matters: a running Mac helper also carries a DevFlow agent, and its presence must not count.
    /// </summary>
    public bool IsIosAgentConnected()
    {
        var result = Cli.Run(MauiTool, ["devflow", "--json", "list"], timeout: TimeSpan.FromSeconds(30));
        return result.ExitCode == 0 && result.StdOut.Contains("\"platform\": \"iOS\"", StringComparison.Ordinal);
    }

    public void Tap(string automationId)
    {
        var result = Invoke("ui", "tap", "--automationId", automationId);
        if (result.ExitCode != 0)
            throw new InvalidOperationException($"tap --automationId {automationId} failed:\n{result.Combined}");
    }

    public void TapText(string text)
    {
        var result = Invoke("ui", "tap", "--text", text);
        if (result.ExitCode != 0)
            throw new InvalidOperationException($"tap --text \"{text}\" failed:\n{result.Combined}");
    }

    // fill requires both positionals; the element id equals the AutomationId when one is set.
    public bool TryFill(string automationId, string text)
        => Invoke("ui", "fill", automationId, text).ExitCode == 0;

    /// <summary>Raw query output for an AutomationId (empty result => element absent).</summary>
    public string Query(string automationId)
        => Invoke("ui", "query", "--automationId", automationId, "--format", "compact").Combined;

    public string QueryText(string text)
        => Invoke("ui", "query", "--text", text, "--format", "compact").Combined;

    public bool Exists(string automationId)
        => Query(automationId).Contains(automationId, StringComparison.Ordinal);

    public bool TextExists(string text)
        => QueryText(text).Contains(text, StringComparison.Ordinal);

    public void WaitFor(string automationId, int timeoutSeconds = 15)
        => WaitUntil(() => Exists(automationId), timeoutSeconds,
            $"element with AutomationId '{automationId}' did not appear");

    public void WaitForGone(string automationId, int timeoutSeconds = 15)
        => WaitUntil(() => !Exists(automationId), timeoutSeconds,
            $"element with AutomationId '{automationId}' did not disappear");

    public void WaitForText(string text, int timeoutSeconds = 15)
        => WaitUntil(() => TextExists(text), timeoutSeconds, $"text '{text}' did not appear");

    public void Screenshot(string outputPath)
        => Invoke("ui", "screenshot", "--output", outputPath);

    public static void WaitUntil(Func<bool> condition, int timeoutSeconds, string description)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        while (DateTime.UtcNow < deadline)
        {
            if (condition())
                return;
            Thread.Sleep(500);
        }
        throw new TimeoutException($"Timed out after {timeoutSeconds}s: {description}");
    }
}
