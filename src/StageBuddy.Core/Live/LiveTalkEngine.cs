using StageBuddy.Core.Models;

namespace StageBuddy.Core.Live;

public sealed class LiveTalkEngine(TalkDocument talk, TimeProvider clock)
{
    private readonly List<BeatTiming> _completedBeatTimings = [];
    private bool _started;
    private bool _isFinished;
    private DateTimeOffset _startedAt;
    private DateTimeOffset _currentBeatStartedAt;
    private DateTimeOffset? _finishedAt;
    private int _beatIndex;
    private int _stepIndex;

    private TimeSpan TotalPlanned => TimeSpan.FromMinutes(talk.Timeline.Sum(b => b.PlannedDurationMinutes));

    public void Start()
    {
        if (_started)
            return;

        _started = true;
        _startedAt = clock.GetUtcNow();
        _currentBeatStartedAt = _startedAt;
        _beatIndex = 0;
        _stepIndex = 0;
    }

    public void NextBeat()
    {
        if (!_started || _isFinished)
            return;

        if (talk.Timeline[_beatIndex].Type != BeatType.Talk)
            return;

        AdvanceToNextBeatOrFinish();
    }

    public void AdvanceStep()
    {
        if (!_started || _isFinished)
            return;

        var beat = talk.Timeline[_beatIndex];
        if (beat.Type != BeatType.Demo)
            return;

        if (_stepIndex >= beat.Steps!.Count - 1)
        {
            AdvanceToNextBeatOrFinish();
        }
        else
        {
            _stepIndex++;
        }
    }

    public void Back()
    {
        if (!_started || _isFinished)
            return;

        var beat = talk.Timeline[_beatIndex];

        if (beat.Type == BeatType.Demo && _stepIndex > 0)
        {
            _stepIndex--;
            return;
        }

        if (_beatIndex == 0)
            return;

        _beatIndex--;
        _currentBeatStartedAt = clock.GetUtcNow();

        var previousBeat = talk.Timeline[_beatIndex];
        _stepIndex = previousBeat.Type == BeatType.Demo ? previousBeat.Steps!.Count - 1 : 0;
    }

    public LiveTalkSnapshot GetSnapshot()
    {
        if (!_started)
            return SnapshotAtStart();

        if (_isFinished)
            return SnapshotAtFinish();

        var beat = talk.Timeline[_beatIndex];
        var isDemo = beat.Type == BeatType.Demo;
        var elapsed = clock.GetUtcNow() - _startedAt;
        var totalPlanned = TotalPlanned;

        var remaining = totalPlanned - elapsed;
        if (remaining < TimeSpan.Zero)
            remaining = TimeSpan.Zero;

        var progress = totalPlanned > TimeSpan.Zero
            ? Math.Clamp(elapsed.TotalMinutes / totalPlanned.TotalMinutes, 0, 1)
            : 0;

        return new LiveTalkSnapshot(
            BeatIndex: _beatIndex,
            StepIndex: isDemo ? _stepIndex : null,
            CurrentBeat: beat,
            CurrentStep: isDemo ? beat.Steps![_stepIndex] : null,
            Elapsed: elapsed,
            Remaining: remaining,
            OverallProgress: progress,
            Pace: PaceCalculator.Compute(elapsed, CumulativePlannedAt(_beatIndex)),
            IsFinished: false,
            CompletedBeatTimings: _completedBeatTimings.ToArray());
    }

    private LiveTalkSnapshot SnapshotAtStart()
    {
        var firstBeat = talk.Timeline[0];
        var isDemo = firstBeat.Type == BeatType.Demo;

        return new LiveTalkSnapshot(
            BeatIndex: 0,
            StepIndex: isDemo ? 0 : null,
            CurrentBeat: firstBeat,
            CurrentStep: isDemo ? firstBeat.Steps![0] : null,
            Elapsed: TimeSpan.Zero,
            Remaining: TotalPlanned,
            OverallProgress: 0,
            Pace: PaceStatus.OnPaceOrAhead,
            IsFinished: false,
            CompletedBeatTimings: []);
    }

    private LiveTalkSnapshot SnapshotAtFinish()
    {
        var lastBeat = talk.Timeline[^1];
        var elapsed = _finishedAt!.Value - _startedAt;

        return new LiveTalkSnapshot(
            BeatIndex: talk.Timeline.Count - 1,
            StepIndex: null,
            CurrentBeat: lastBeat,
            CurrentStep: null,
            Elapsed: elapsed,
            Remaining: TimeSpan.Zero,
            OverallProgress: 1,
            Pace: PaceCalculator.Compute(elapsed, TotalPlanned),
            IsFinished: true,
            CompletedBeatTimings: _completedBeatTimings.ToArray());
    }

    private void AdvanceToNextBeatOrFinish()
    {
        var now = clock.GetUtcNow();
        var beat = talk.Timeline[_beatIndex];
        _completedBeatTimings.Add(new BeatTiming(beat.Id, TimeSpan.FromMinutes(beat.PlannedDurationMinutes), now - _currentBeatStartedAt));

        if (_beatIndex == talk.Timeline.Count - 1)
        {
            _isFinished = true;
            _finishedAt = now;
            return;
        }

        _beatIndex++;
        _stepIndex = 0;
        _currentBeatStartedAt = now;
    }

    private TimeSpan CumulativePlannedAt(int index) =>
        TimeSpan.FromMinutes(talk.Timeline.Take(index + 1).Sum(b => b.PlannedDurationMinutes));
}
