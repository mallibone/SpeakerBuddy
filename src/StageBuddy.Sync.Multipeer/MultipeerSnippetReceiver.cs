using System.Text;
using System.Text.Json;
using Foundation;
using MultipeerConnectivity;
using StageBuddy.Core.Serialization;
using StageBuddy.Core.Sync;
using StageBuddy.Sync.Multipeer.Delegates;

namespace StageBuddy.Sync.Multipeer;

public sealed class MultipeerSnippetReceiver : ISnippetReceiver, ISessionEventSink, IAdvertiserEventSink, IDisposable
{
    private readonly IPairingTokenStore _tokenStore;
    private readonly Action<Action> _dispatchToUi;
    private readonly PairingStateMachine _stateMachine;
    private readonly MCPeerID _peerId;
    private readonly SessionDelegate _sessionDelegate;
    private readonly AdvertiserDelegate _advertiserDelegate;
    private readonly MCNearbyServiceAdvertiser _advertiser;

    private MCSession _session;
    private string? _acceptedToken;

    public MultipeerSnippetReceiver(string displayName, IPairingTokenStore tokenStore, Action<Action> dispatchToUi, TimeProvider clock)
    {
        _tokenStore = tokenStore;
        _dispatchToUi = dispatchToUi;
        _acceptedToken = tokenStore.Load();

        _stateMachine = new PairingStateMachine(clock);
        _stateMachine.StateChanged += (_, state) => _dispatchToUi(() => StateChanged?.Invoke(this, state));

        _peerId = MultipeerCommon.GetOrCreatePeerId(displayName);
        _session = MultipeerCommon.CreateSession(_peerId);
        _sessionDelegate = new SessionDelegate(this);
        _session.Delegate = _sessionDelegate;

        _advertiserDelegate = new AdvertiserDelegate(this);
        _advertiser = new MCNearbyServiceAdvertiser(_peerId, null, MultipeerConstants.ServiceType);
        _advertiser.Delegate = _advertiserDelegate;
    }

    public ConnectionState State => _stateMachine.State;
    public event EventHandler<ConnectionState>? StateChanged;
    public event EventHandler<Snippet>? SnippetReceived;
    public event EventHandler<PairingRequest>? UnknownPeerPairingRequested;

    public void StartAdvertising()
    {
        _stateMachine.Start();
        _advertiser.StartAdvertisingPeer();
    }

    public void StopAdvertising()
    {
        _advertiser.StopAdvertisingPeer();
        _stateMachine.Stop();
    }

    public void RePair()
    {
        _acceptedToken = null;
        _tokenStore.Clear();
    }

    void IAdvertiserEventSink.OnInvitation(MCPeerID peerId, NSData? context, MCNearbyServiceAdvertiserInvitationHandler invitationHandler)
    {
        var token = context is not null ? Encoding.UTF8.GetString(context.ToArray()) : null;

        if (token is not null && token == _acceptedToken)
        {
            invitationHandler(true, _session);
            return;
        }

        _dispatchToUi(() =>
        {
            UnknownPeerPairingRequested?.Invoke(this, new PairingRequest(peerId.DisplayName, accepted =>
            {
                if (accepted && token is not null)
                {
                    _acceptedToken = token;
                    _tokenStore.Save(token);
                }

                invitationHandler(accepted, _session);
            }));
        });
    }

    void IAdvertiserEventSink.OnDidNotStartAdvertising(NSError error) =>
        _dispatchToUi(() => _stateMachine.PermissionDenied());

    void ISessionEventSink.OnStateChanged(MCSessionState state, MCPeerID peer)
    {
        _dispatchToUi(() =>
        {
            switch (state)
            {
                case MCSessionState.Connected:
                    _stateMachine.SessionConnected();
                    break;
                case MCSessionState.NotConnected:
                    _stateMachine.SessionDisconnected();
                    RecreateSession();
                    break;
            }
        });
    }

    void ISessionEventSink.OnDataReceived(NSData data, MCPeerID fromPeer)
    {
        SyncEnvelope? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize(data.ToArray(), TalkJsonContext.Default.SyncEnvelope);
        }
        catch (JsonException)
        {
            return;
        }

        if (envelope is not { Type: "snippet", Payload: not null })
            return;

        var snippet = new Snippet(envelope.Payload.Text, envelope.Payload.Language, envelope.Payload.TalkTitle, envelope.Payload.StepId);
        _dispatchToUi(() => SnippetReceived?.Invoke(this, snippet));
    }

    private void RecreateSession()
    {
        _session = MultipeerCommon.CreateSession(_peerId);
        _session.Delegate = _sessionDelegate;
    }

    public void Dispose()
    {
        _advertiser.StopAdvertisingPeer();
        _session.Disconnect();
    }
}
