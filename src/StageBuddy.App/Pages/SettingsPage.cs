using StageBuddy.Core.Sync;
using StageBuddy.Sync.Multipeer;

namespace StageBuddy.App.Pages;

class SettingsPageState
{
    public ConnectionState PairingState { get; set; }
    public bool IsRehearsalMode { get; set; }
}

class SettingsPage : Component<SettingsPageState>
{
    private ISnippetSender Sender => Services.GetRequiredService<ISnippetSender>();
    private IRehearsalModeToggle RehearsalMode => Services.GetRequiredService<IRehearsalModeToggle>();

    protected override void OnMounted()
    {
        base.OnMounted();
        SetState(s =>
        {
            s.PairingState = Sender.State;
            s.IsRehearsalMode = RehearsalMode.IsRehearsalMode;
        });
        Sender.StateChanged += OnSenderStateChanged;
    }

    protected override void OnWillUnmount()
    {
        Sender.StateChanged -= OnSenderStateChanged;
        base.OnWillUnmount();
    }

    private void OnSenderStateChanged(object? sender, ConnectionState state) =>
        SetState(s => s.PairingState = state);

    public override VisualNode Render()
        => ContentPage(
            VStack(
                RenderPairingSection(),
                RenderRehearsalSection(),

                Button("Done")
                    .OnClicked(Done)
                    .ImageSource(Icons.Glyph(Icons.Check, ApplicationTheme.OnAccent, 18))
                    .HeightRequest(52)
                    .AutomationId("SettingsDoneButton")
            )
            .Spacing(14)
            .Padding(16)
        )
        .Title("Settings");

    private VisualNode RenderPairingSection()
        => Border(
            VStack(
                Label("Mac Pairing")
                    .FontSize(16)
                    .FontFamily("OpenSansSemibold"),

                HStack(
                    BoxView()
                        .WidthRequest(10)
                        .HeightRequest(10)
                        .CornerRadius(5)
                        .BackgroundColor(StateDotColor(State.PairingState))
                        .VCenter(),
                    Label(DescribeState(State.PairingState))
                        .TextColor(ApplicationTheme.TextSecondary)
                        .VCenter()
                        .AutomationId("SettingsPairingStateLabel")
                )
                .Spacing(8),

                Button("Re-pair")
                    .OnClicked(RePair)
                    .ThemeKey("Secondary")
                    .AutomationId("SettingsRePairButton")
            )
            .Spacing(12)
        )
        .BackgroundColor(ApplicationTheme.Card)
        .Padding(16);

    private VisualNode RenderRehearsalSection()
        => Border(
            VStack(
                Label("Rehearsal Mode")
                    .FontSize(16)
                    .FontFamily("OpenSansSemibold"),

                Label(State.IsRehearsalMode
                    ? "On — Copy sends to this device's clipboard instead of the Mac"
                    : "Off — Copy sends to the paired Mac")
                    .TextColor(ApplicationTheme.TextSecondary)
                    .AutomationId("SettingsRehearsalStateLabel"),

                Button(State.IsRehearsalMode ? "Turn Off" : "Turn On")
                    .OnClicked(ToggleRehearsalMode)
                    .ImageSource(Icons.Glyph(Icons.Presenter, ApplicationTheme.Accent, 18))
                    .ThemeKey("Secondary")
                    .AutomationId("SettingsRehearsalToggleButton")
            )
            .Spacing(12)
        )
        .BackgroundColor(ApplicationTheme.Card)
        .Padding(16);

    private static Color StateDotColor(ConnectionState state) => state switch
    {
        ConnectionState.Connected => ApplicationTheme.PaceOnTrack,
        ConnectionState.Connecting or ConnectionState.Discovering => ApplicationTheme.PaceTolerance,
        _ => ApplicationTheme.TextSecondary
    };

    private static string DescribeState(ConnectionState state) => state switch
    {
        ConnectionState.Connected => "Connected to Mac",
        ConnectionState.Connecting => "Connecting…",
        ConnectionState.Discovering => "Looking for your Mac…",
        _ => "Not paired"
    };

    private async void Done() => await Navigation!.PopAsync();

    private void RePair()
    {
        var underlyingSender = Sender is RehearsalAwareSnippetSender rehearsalAware ? rehearsalAware.LiveSender : Sender;

        if (underlyingSender is MultipeerSnippetSender multipeerSender)
        {
            multipeerSender.RePair();
            return;
        }

        underlyingSender.StopPairing();
        underlyingSender.StartPairing();
    }

    private void ToggleRehearsalMode()
    {
        RehearsalMode.SetRehearsalMode(!State.IsRehearsalMode);
        SetState(s => s.IsRehearsalMode = RehearsalMode.IsRehearsalMode);
    }
}
