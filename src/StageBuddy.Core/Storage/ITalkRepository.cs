using StageBuddy.Core.Models;

namespace StageBuddy.Core.Storage;

public interface ITalkRepository
{
    Task SaveAsync(string talkId, TalkDocument talk, CancellationToken ct = default);
    Task<TalkDocument?> LoadAsync(string talkId, CancellationToken ct = default);
    Task<IReadOnlyList<TalkSummary>> ListAsync(CancellationToken ct = default);
    Task DeleteAsync(string talkId, CancellationToken ct = default);
}
