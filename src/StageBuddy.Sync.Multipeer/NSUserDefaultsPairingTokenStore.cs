using Foundation;

namespace StageBuddy.Sync.Multipeer;

public sealed class NSUserDefaultsPairingTokenStore(string key) : IPairingTokenStore
{
    public string? Load() => NSUserDefaults.StandardUserDefaults.StringForKey(key);

    public void Save(string token) => NSUserDefaults.StandardUserDefaults.SetString(token, key);

    public void Clear() => NSUserDefaults.StandardUserDefaults.RemoveObject(key);
}
