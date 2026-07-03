using Foundation;
using MultipeerConnectivity;

namespace StageBuddy.Sync.Multipeer.Delegates;

internal interface IAdvertiserEventSink
{
    void OnInvitation(MCPeerID peerId, NSData? context, MCNearbyServiceAdvertiserInvitationHandler invitationHandler);
    void OnDidNotStartAdvertising(NSError error);
}

internal sealed class AdvertiserDelegate(IAdvertiserEventSink owner) : MCNearbyServiceAdvertiserDelegate
{
    public override void DidReceiveInvitationFromPeer(MCNearbyServiceAdvertiser advertiser, MCPeerID peerID, NSData? context, MCNearbyServiceAdvertiserInvitationHandler invitationHandler) =>
        owner.OnInvitation(peerID, context, invitationHandler);

    public override void DidNotStartAdvertisingPeer(MCNearbyServiceAdvertiser advertiser, NSError error) =>
        owner.OnDidNotStartAdvertising(error);
}
