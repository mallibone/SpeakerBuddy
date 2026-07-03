using Foundation;
using MultipeerConnectivity;

namespace StageBuddy.Sync.Multipeer.Delegates;

internal interface IBrowserEventSink
{
    void OnPeerFound(MCPeerID peerId, NSDictionary? info);
    void OnPeerLost(MCPeerID peerId);
    void OnDidNotStartBrowsing(NSError error);
}

internal sealed class BrowserDelegate(IBrowserEventSink owner) : MCNearbyServiceBrowserDelegate
{
    public override void FoundPeer(MCNearbyServiceBrowser browser, MCPeerID peerID, NSDictionary? info) =>
        owner.OnPeerFound(peerID, info);

    public override void LostPeer(MCNearbyServiceBrowser browser, MCPeerID peerID) =>
        owner.OnPeerLost(peerID);

    public override void DidNotStartBrowsingForPeers(MCNearbyServiceBrowser browser, NSError error) =>
        owner.OnDidNotStartBrowsing(error);
}
