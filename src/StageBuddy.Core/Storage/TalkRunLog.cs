using StageBuddy.Core.Live;

namespace StageBuddy.Core.Storage;

public sealed record TalkRunLog(
    string TalkId,
    DateTimeOffset StartedAt,
    IReadOnlyList<BeatTiming> BeatTimings,
    TimeSpan TotalPlanned,
    TimeSpan TotalActual);
