namespace StageBuddy.Core.Sync;

public interface ISnippetReceiver
{
    ConnectionState State { get; }
    event EventHandler<ConnectionState>? StateChanged;
    event EventHandler<Snippet>? SnippetReceived;
    void StartAdvertising();
    void StopAdvertising();
}
