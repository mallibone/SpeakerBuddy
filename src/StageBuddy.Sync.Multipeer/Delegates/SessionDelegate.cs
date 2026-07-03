using Foundation;
using MultipeerConnectivity;

namespace StageBuddy.Sync.Multipeer.Delegates;

internal interface ISessionEventSink
{
    void OnStateChanged(MCSessionState state, MCPeerID peer);
    void OnDataReceived(NSData data, MCPeerID fromPeer);
}

internal sealed class SessionDelegate(ISessionEventSink owner) : MCSessionDelegate
{
    public override void DidChangeState(MCSession session, MCPeerID peerID, MCSessionState state) =>
        owner.OnStateChanged(state, peerID);

    public override void DidReceiveData(MCSession session, NSData data, MCPeerID peerID) =>
        owner.OnDataReceived(data, peerID);

    public override void DidReceiveStream(MCSession session, NSInputStream stream, string streamName, MCPeerID peerID)
    {
    }

    public override void DidStartReceivingResource(MCSession session, string resourceName, MCPeerID fromPeer, NSProgress progress)
    {
    }

    public override void DidFinishReceivingResource(MCSession session, string resourceName, MCPeerID fromPeer, NSUrl? localUrl, NSError? error)
    {
    }
}
