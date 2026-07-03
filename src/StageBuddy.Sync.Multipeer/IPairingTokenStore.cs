namespace StageBuddy.Sync.Multipeer;

public interface IPairingTokenStore
{
    string? Load();
    void Save(string token);
    void Clear();
}
