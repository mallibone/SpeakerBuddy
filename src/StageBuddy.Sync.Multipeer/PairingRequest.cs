namespace StageBuddy.Sync.Multipeer;

public sealed record PairingRequest(string PeerDisplayName, Action<bool> Respond);
