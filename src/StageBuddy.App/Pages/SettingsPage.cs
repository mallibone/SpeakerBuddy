using StageBuddy.Core.Sync;
using StageBuddy.Sync.Multipeer;

namespace StageBuddy.App.Pages;

class SettingsPageState
{
    public ConnectionState PairingState { get; set; }
}

class SettingsPage : Component<SettingsPageState>
{
    private ISnippetSender Sender => Services.GetRequiredService<ISnippetSender>();

    protected override void OnMounted()
    {
        base.OnMounted();
        SetState(s => s.PairingState = Sender.State);
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

                Button("Re-pair").OnClicked(RePair)
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
        if (Sender is MultipeerSnippetSender multipeerSender)
        {
            multipeerSender.RePair();
            return;
        }

        Sender.StopPairing();
        Sender.StartPairing();
    }
}
