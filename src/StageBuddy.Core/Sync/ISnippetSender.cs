namespace StageBuddy.Core.Sync;

public interface ISnippetSender
{
    ConnectionState State { get; }
    event EventHandler<ConnectionState>? StateChanged;
    void StartPairing();
    void StopPairing();
    Task<SendResult> SendAsync(Snippet snippet, CancellationToken ct = default);
}
