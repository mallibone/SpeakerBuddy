using Foundation;
using StageBuddy.Core.Sync;
using StageBuddy.MacHelper.Services;
using StageBuddy.Sync.Multipeer;

namespace StageBuddy.MacHelper.Pages;

class HelperHomePageState
{
    public ConnectionState Connection { get; set; } = ConnectionState.Disconnected;
    public string? LastSnippetPreview { get; set; }
    public DateTimeOffset? LastReceivedAt { get; set; }
    public PairingRequest? PendingPairingRequest { get; set; }
}

class HelperHomePage : Component<HelperHomePageState>
{
    private ISnippetReceiver Receiver => Services.GetRequiredService<ISnippetReceiver>();
    private NSObject? _activityToken;

    protected override void OnMounted()
    {
        base.OnMounted();

        Receiver.StateChanged += OnConnectionStateChanged;
        Receiver.SnippetReceived += OnSnippetReceived;
        if (Receiver is MultipeerSnippetReceiver multipeerReceiver)
            multipeerReceiver.UnknownPeerPairingRequested += OnPairingRequested;

        Receiver.StartAdvertising();
    }

    protected override void OnWillUnmount()
    {
        Receiver.StateChanged -= OnConnectionStateChanged;
        Receiver.SnippetReceived -= OnSnippetReceived;
        if (Receiver is MultipeerSnippetReceiver multipeerReceiver)
            multipeerReceiver.UnknownPeerPairingRequested -= OnPairingRequested;

        Receiver.StopAdvertising();

        base.OnWillUnmount();
    }

    private void OnConnectionStateChanged(object? sender, ConnectionState state)
    {
        SetState(s => s.Connection = state);

        if (state == ConnectionState.Connected)
        {
            _activityToken ??= NSProcessInfo.ProcessInfo.BeginActivity(
                NSActivityOptions.UserInitiated | NSActivityOptions.IdleSystemSleepDisabled,
                "StageBuddy live session");
        }
        else if (_activityToken is not null)
        {
            NSProcessInfo.ProcessInfo.EndActivity(_activityToken);
            _activityToken = null;
        }
    }

    private async void OnSnippetReceived(object? sender, Snippet snippet)
    {
        await PasteboardWriter.WriteAsync(snippet.Text);

        SetState(s =>
        {
            s.LastSnippetPreview = snippet.Text;
            s.LastReceivedAt = DateTimeOffset.Now;
        });
    }

    private void OnPairingRequested(object? sender, PairingRequest request) =>
        SetState(s => s.PendingPairingRequest = request);

    public override VisualNode Render()
        => ContentPage(
            VStack(
                RenderStatus(),
                RenderLastSnippet(),
                State.PendingPairingRequest is { } request
                    ? RenderPairingConfirmation(request)
                    : null
            )
            .Spacing(16)
            .Padding(24)
        )
        .Title("Stage Buddy Helper");

    private VisualNode RenderStatus()
        => Label(State.Connection == ConnectionState.Connected
                ? "Connected to iPhone"
                : "Waiting for Stage Buddy…")
            .TextColor(State.Connection == ConnectionState.Connected ? Colors.Green : Colors.Gray)
            .FontSize(20);

    private VisualNode RenderLastSnippet()
        => State.LastReceivedAt is null
            ? Label("No snippets received yet")
                .TextColor(Colors.Gray)
            : VStack(
                Label($"Last received {State.LastReceivedAt:T}")
                    .TextColor(Colors.Gray)
                    .FontSize(12),
                Label(State.LastSnippetPreview ?? string.Empty)
                    .FontFamily("Courier")
                    .LineBreakMode(LineBreakMode.TailTruncation)
                    .MaxLines(3)
            );

    private VisualNode RenderPairingConfirmation(PairingRequest request)
        => VStack(
            Label($"Pair with \"{request.PeerDisplayName}\"?")
                .FontAttributes(FontAttributes.Bold),
            HStack(
                Button("Accept").OnClicked(() => RespondToPairing(request, true)),
                Button("Decline").OnClicked(() => RespondToPairing(request, false))
            )
            .Spacing(12)
        )
        .Spacing(8)
        .Padding(12)
        .BackgroundColor(Colors.LightYellow);

    private void RespondToPairing(PairingRequest request, bool accepted)
    {
        request.Respond(accepted);
        SetState(s => s.PendingPairingRequest = null);
    }
}
