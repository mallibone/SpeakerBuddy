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
                Label("Mac Pairing")
                    .FontSize(18)
                    .FontAttributes(FontAttributes.Bold),

                Label(DescribeState(State.PairingState))
                    .TextColor(State.PairingState == ConnectionState.Connected ? Colors.Green : Colors.Gray),

                Button("Re-pair").OnClicked(RePair),

                Label("Rehearsal Mode")
                    .FontSize(18)
                    .FontAttributes(FontAttributes.Bold),

                Label(State.IsRehearsalMode
                    ? "On — Copy sends to this device's clipboard instead of the Mac"
                    : "Off — Copy sends to the paired Mac")
                    .TextColor(Colors.Gray),

                Button(State.IsRehearsalMode ? "Turn Off" : "Turn On").OnClicked(ToggleRehearsalMode)
            )
            .Spacing(12)
            .Padding(16)
        )
        .Title("Settings");

    private static string DescribeState(ConnectionState state) => state switch
    {
        ConnectionState.Connected => "Connected to Mac",
        ConnectionState.Connecting => "Connecting…",
        ConnectionState.Discovering => "Looking for your Mac…",
        _ => "Not paired"
    };

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
