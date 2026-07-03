using StageBuddy.Core.Models;

namespace StageBuddy.Core.Live;

public sealed record LiveTalkSnapshot(
    int BeatIndex,
    int? StepIndex,
    Beat CurrentBeat,
    DemoStep? CurrentStep,
    TimeSpan Elapsed,
    TimeSpan Remaining,
    double OverallProgress,
    PaceStatus Pace,
    bool IsFinished,
    IReadOnlyList<BeatTiming> CompletedBeatTimings);
