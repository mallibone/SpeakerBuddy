namespace StageBuddy.Core.Storage;

public interface ITalkRunLogRepository
{
    Task SaveAsync(TalkRunLog runLog, CancellationToken ct = default);
    Task<IReadOnlyList<TalkRunLog>> ListAsync(string talkId, CancellationToken ct = default);
    Task<TalkRunLog?> GetLatestAsync(string talkId, CancellationToken ct = default);
}
