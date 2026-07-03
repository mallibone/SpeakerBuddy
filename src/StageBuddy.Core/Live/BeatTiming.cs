namespace StageBuddy.Core.Live;

public sealed record BeatTiming(string BeatId, TimeSpan Planned, TimeSpan Actual);
