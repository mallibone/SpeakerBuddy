namespace StageBuddy.Core.Models;

public sealed record Beat(
    string Id,
    BeatType Type,
    string Title,
    int PlannedDurationMinutes,
    IReadOnlyList<DemoStep>? Steps);
