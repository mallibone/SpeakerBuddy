using Foundation;
using MultipeerConnectivity;
using ObjCRuntime;
using Security;

namespace StageBuddy.Sync.Multipeer;

public static class MultipeerCommon
{
    private const string PeerIdDefaultsKey = "StageBuddy.MultipeerPeerId";

    public static MCPeerID GetOrCreatePeerId(string displayName)
    {
        var defaults = NSUserDefaults.StandardUserDefaults;
        var existingData = defaults.DataForKey(PeerIdDefaultsKey);

        if (existingData is not null &&
            NSKeyedUnarchiver.GetUnarchivedObject(new Class(typeof(MCPeerID)), existingData, out _) is MCPeerID restored)
        {
            return restored;
        }

        var peerId = new MCPeerID(displayName);
        var archived = NSKeyedArchiver.GetArchivedData(peerId, true, out _);
        if (archived is not null)
            defaults.SetValueForKey(archived, new NSString(PeerIdDefaultsKey));

        return peerId;
    }

    public static MCSession CreateSession(MCPeerID peerId) =>
        // Apple's API allows a nil securityIdentity (no client certificate); the .NET for iOS
        // binding's nullability annotation on this parameter doesn't reflect that.
        new(peerId, identity: null!, MCEncryptionPreference.Required);
}
