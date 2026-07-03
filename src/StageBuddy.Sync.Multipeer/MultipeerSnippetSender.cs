using System.Text.Json;
using Foundation;
using MultipeerConnectivity;
using StageBuddy.Core.Serialization;
using StageBuddy.Core.Sync;
using StageBuddy.Sync.Multipeer.Delegates;

namespace StageBuddy.Sync.Multipeer;

public sealed class MultipeerSnippetSender : ISnippetSender, ISessionEventSink, IBrowserEventSink, IDisposable
{
    private const int MaxPayloadBytes = 1024 * 1024;

    private readonly IPairingTokenStore _tokenStore;
    private readonly Action<Action> _dispatchToUi;
    private readonly PairingStateMachine _stateMachine;
    private readonly MCPeerID _peerId;
    private readonly SessionDelegate _sessionDelegate;
    private readonly BrowserDelegate _browserDelegate;
    private readonly MCNearbyServiceBrowser _browser;

    private MCSession _session;
    private string _pairingToken;

    public MultipeerSnippetSender(string displayName, IPairingTokenStore tokenStore, Action<Action> dispatchToUi, TimeProvider clock)
    {
        _tokenStore = tokenStore;
        _dispatchToUi = dispatchToUi;

        _pairingToken = tokenStore.Load() ?? Guid.NewGuid().ToString("N");
        tokenStore.Save(_pairingToken);

        _stateMachine = new PairingStateMachine(clock);
        _stateMachine.StateChanged += (_, state) => _dispatchToUi(() => StateChanged?.Invoke(this, state));

        _peerId = MultipeerCommon.GetOrCreatePeerId(displayName);
        _session = MultipeerCommon.CreateSession(_peerId);
        _sessionDelegate = new SessionDelegate(this);
        _session.Delegate = _sessionDelegate;

        _browserDelegate = new BrowserDelegate(this);
        _browser = new MCNearbyServiceBrowser(_peerId, MultipeerConstants.ServiceType);
        _browser.Delegate = _browserDelegate;
    }

    public ConnectionState State => _stateMachine.State;
    public event EventHandler<ConnectionState>? StateChanged;

    public void StartPairing()
    {
        if (State != ConnectionState.Disconnected)
            return;

        _stateMachine.Start();
        _browser.StartBrowsingForPeers();
    }

    public void StopPairing()
    {
        _browser.StopBrowsingForPeers();
        _stateMachine.Stop();
    }

    /// <summary>
    /// Forces a fresh pairing token so the Mac no longer recognizes this device and shows
    /// its confirm dialog again on the next connection attempt.
    /// </summary>
    public void RePair()
    {
        StopPairing();

        _tokenStore.Clear();
        _pairingToken = Guid.NewGuid().ToString("N");
        _tokenStore.Save(_pairingToken);

        StartPairing();
    }

    public Task<SendResult> SendAsync(Snippet snippet, CancellationToken ct = default)
    {
        if (_session.ConnectedPeers is not { Length: > 0 })
            return Task.FromResult(SendResult.NotConnected);

        var envelope = new SyncEnvelope(1, "snippet", new SnippetPayload(snippet.Text, snippet.Language, snippet.TalkTitle, snippet.StepId));
        var bytes = JsonSerializer.SerializeToUtf8Bytes(envelope, TalkJsonContext.Default.SyncEnvelope);

        if (bytes.Length > MaxPayloadBytes)
            return Task.FromResult(SendResult.Failed);

        var data = NSData.FromArray(bytes);
        var sent = _session.SendData(data, _session.ConnectedPeers, MCSessionSendDataMode.Reliable, out var error);

        return Task.FromResult(sent && error is null ? SendResult.Sent : SendResult.Failed);
    }

    void IBrowserEventSink.OnPeerFound(MCPeerID peerId, NSDictionary? info)
    {
        _dispatchToUi(() =>
        {
            _stateMachine.PeerFound();
            if (_stateMachine.State != ConnectionState.Connecting)
                return;

            var tokenData = NSData.FromString(_pairingToken, NSStringEncoding.UTF8);
            _browser.InvitePeer(peerId, _session, tokenData, 15);
        });
    }

    void IBrowserEventSink.OnPeerLost(MCPeerID peerId)
    {
        _dispatchToUi(() =>
        {
            var previousDelay = _stateMachine.LastBackoffDelay;
            _stateMachine.PeerLost();
            if (_stateMachine.LastBackoffDelay != previousDelay)
                ScheduleReconnect(_stateMachine.LastBackoffDelay!.Value);
        });
    }

    void IBrowserEventSink.OnDidNotStartBrowsing(NSError error) =>
        _dispatchToUi(() => _stateMachine.PermissionDenied());

    void ISessionEventSink.OnStateChanged(MCSessionState state, MCPeerID peer)
    {
        _dispatchToUi(() =>
        {
            switch (state)
            {
                case MCSessionState.Connected:
                    _stateMachine.SessionConnected();
                    _browser.StopBrowsingForPeers();
                    break;
                case MCSessionState.NotConnected:
                    var previousDelay = _stateMachine.LastBackoffDelay;
                    _stateMachine.SessionDisconnected();
                    if (_stateMachine.LastBackoffDelay != previousDelay)
                        ScheduleReconnect(_stateMachine.LastBackoffDelay!.Value);
                    break;
            }
        });
    }

    void ISessionEventSink.OnDataReceived(NSData data, MCPeerID fromPeer)
    {
        // The sender doesn't expect inbound data yet; ack/ping are reserved for later.
    }

    private void ScheduleReconnect(TimeSpan delay)
    {
        _browser.StopBrowsingForPeers();
        _session = MultipeerCommon.CreateSession(_peerId);
        _session.Delegate = _sessionDelegate;

        _ = Task.Delay(delay).ContinueWith(_ => _dispatchToUi(() =>
        {
            if (_stateMachine.State == ConnectionState.Discovering)
                _browser.StartBrowsingForPeers();
        }));
    }

    public void Dispose()
    {
        _browser.StopBrowsingForPeers();
        _session.Disconnect();
    }
}
