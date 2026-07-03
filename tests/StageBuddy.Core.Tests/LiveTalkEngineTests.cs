using Microsoft.Extensions.Time.Testing;
using StageBuddy.Core.Live;
using StageBuddy.Core.Models;

namespace StageBuddy.Core.Tests;

public class LiveTalkEngineTests
{
    private static TalkDocument SampleTalk() => new(
        new SessionInfo("A Talk", 16),
        [
            new Beat("beat-1", BeatType.Talk, "Intro", 5, null),
            new Beat("beat-2", BeatType.Demo, "Demo", 8,
            [
                new DemoStep("step-1", "First", "code1", null, null),
                new DemoStep("step-2", "Second", "code2", null, null)
            ]),
            new Beat("beat-3", BeatType.Talk, "Wrap up", 3, null)
        ]);

    private static (LiveTalkEngine Engine, FakeTimeProvider Clock) CreateEngine()
    {
        var clock = new FakeTimeProvider();
        return (new LiveTalkEngine(SampleTalk(), clock), clock);
    }

    [Fact]
    public void NotStarted_ShowsFirstBeat_ZeroElapsed_NotFinished()
    {
        var (engine, _) = CreateEngine();

        var snapshot = engine.GetSnapshot();

        Assert.Equal(0, snapshot.BeatIndex);
        Assert.Null(snapshot.StepIndex);
        Assert.Equal("beat-1", snapshot.CurrentBeat.Id);
        Assert.Equal(TimeSpan.Zero, snapshot.Elapsed);
        Assert.Equal(TimeSpan.FromMinutes(16), snapshot.Remaining);
        Assert.False(snapshot.IsFinished);
        Assert.Empty(snapshot.CompletedBeatTimings);
    }

    [Fact]
    public void Start_BeginsTimer()
    {
        var (engine, clock) = CreateEngine();

        engine.Start();
        clock.Advance(TimeSpan.FromMinutes(2));

        var snapshot = engine.GetSnapshot();

        Assert.Equal(TimeSpan.FromMinutes(2), snapshot.Elapsed);
    }

    [Fact]
    public void Start_CalledTwice_IsNoOp()
    {
        var (engine, clock) = CreateEngine();

        engine.Start();
        clock.Advance(TimeSpan.FromMinutes(2));
        engine.Start();

        var snapshot = engine.GetSnapshot();

        Assert.Equal(TimeSpan.FromMinutes(2), snapshot.Elapsed);
    }

    [Fact]
    public void NextBeat_OnTalkBeat_AdvancesAndRecordsTiming()
    {
        var (engine, clock) = CreateEngine();
        engine.Start();
        clock.Advance(TimeSpan.FromMinutes(4));

        engine.NextBeat();

        var snapshot = engine.GetSnapshot();
        Assert.Equal(1, snapshot.BeatIndex);
        Assert.Equal("beat-2", snapshot.CurrentBeat.Id);
        Assert.Equal(0, snapshot.StepIndex);

        var timing = Assert.Single(snapshot.CompletedBeatTimings);
        Assert.Equal("beat-1", timing.BeatId);
        Assert.Equal(TimeSpan.FromMinutes(5), timing.Planned);
        Assert.Equal(TimeSpan.FromMinutes(4), timing.Actual);
    }

    [Fact]
    public void NextBeat_OnDemoBeat_IsNoOp()
    {
        var (engine, clock) = CreateEngine();
        engine.Start();
        engine.NextBeat(); // beat-1 -> beat-2 (demo)

        engine.NextBeat(); // should be ignored, beat-2 is a demo beat

        var snapshot = engine.GetSnapshot();
        Assert.Equal(1, snapshot.BeatIndex);
        Assert.Equal("beat-2", snapshot.CurrentBeat.Id);
    }

    [Fact]
    public void AdvanceStep_MovesWithinDemoBeat()
    {
        var (engine, _) = CreateEngine();
        engine.Start();
        engine.NextBeat(); // now on beat-2, step 0

        engine.AdvanceStep();

        var snapshot = engine.GetSnapshot();
        Assert.Equal(1, snapshot.BeatIndex);
        Assert.Equal(1, snapshot.StepIndex);
        Assert.Equal("step-2", snapshot.CurrentStep!.Id);
    }

    [Fact]
    public void AdvanceStep_OnLastStep_RollsIntoNextBeat()
    {
        var (engine, clock) = CreateEngine();
        engine.Start();
        engine.NextBeat(); // beat-2, step 0
        clock.Advance(TimeSpan.FromMinutes(3));
        engine.AdvanceStep(); // beat-2, step 1
        clock.Advance(TimeSpan.FromMinutes(4));

        engine.AdvanceStep(); // rolls into beat-3

        var snapshot = engine.GetSnapshot();
        Assert.Equal(2, snapshot.BeatIndex);
        Assert.Equal("beat-3", snapshot.CurrentBeat.Id);
        Assert.Null(snapshot.StepIndex);

        Assert.Equal(2, snapshot.CompletedBeatTimings.Count);
        var beat2Timing = snapshot.CompletedBeatTimings[1];
        Assert.Equal("beat-2", beat2Timing.BeatId);
        Assert.Equal(TimeSpan.FromMinutes(7), beat2Timing.Actual);
    }

    [Fact]
    public void AdvanceStep_OnTalkBeat_IsNoOp()
    {
        var (engine, _) = CreateEngine();
        engine.Start();

        engine.AdvanceStep();

        var snapshot = engine.GetSnapshot();
        Assert.Equal(0, snapshot.BeatIndex);
        Assert.Equal("beat-1", snapshot.CurrentBeat.Id);
    }

    [Fact]
    public void Back_WithinDemoSteps_MovesToPreviousStep()
    {
        var (engine, _) = CreateEngine();
        engine.Start();
        engine.NextBeat(); // beat-2, step 0
        engine.AdvanceStep(); // beat-2, step 1

        engine.Back();

        var snapshot = engine.GetSnapshot();
        Assert.Equal(1, snapshot.BeatIndex);
        Assert.Equal(0, snapshot.StepIndex);
    }

    [Fact]
    public void Back_AcrossBeatBoundary_FromDemoFirstStep_MovesToPreviousBeat()
    {
        var (engine, _) = CreateEngine();
        engine.Start();
        engine.NextBeat(); // beat-2 (demo), step 0

        engine.Back();

        var snapshot = engine.GetSnapshot();
        Assert.Equal(0, snapshot.BeatIndex);
        Assert.Equal("beat-1", snapshot.CurrentBeat.Id);
        Assert.Null(snapshot.StepIndex);
    }

    [Fact]
    public void Back_FromTalkBeat_LandsOnLastStepOfPreviousDemoBeat()
    {
        var (engine, _) = CreateEngine();
        engine.Start();
        engine.NextBeat(); // beat-2, step 0
        engine.AdvanceStep(); // beat-2, step 1
        engine.AdvanceStep(); // beat-3 (talk)

        engine.Back();

        var snapshot = engine.GetSnapshot();
        Assert.Equal(1, snapshot.BeatIndex);
        Assert.Equal("beat-2", snapshot.CurrentBeat.Id);
        Assert.Equal(1, snapshot.StepIndex);
        Assert.Equal("step-2", snapshot.CurrentStep!.Id);
    }

    [Fact]
    public void Back_AtPosition0_IsNoOp()
    {
        var (engine, _) = CreateEngine();
        engine.Start();

        engine.Back();

        var snapshot = engine.GetSnapshot();
        Assert.Equal(0, snapshot.BeatIndex);
        Assert.Equal("beat-1", snapshot.CurrentBeat.Id);
    }

    [Fact]
    public void Back_DoesNotRemoveAlreadyRecordedBeatTiming()
    {
        var (engine, _) = CreateEngine();
        engine.Start();
        engine.NextBeat(); // beat-1 completed, now on beat-2

        engine.Back();

        var snapshot = engine.GetSnapshot();
        Assert.Single(snapshot.CompletedBeatTimings);
    }

    [Fact]
    public void FinishDetection_AfterLastBeat()
    {
        var (engine, clock) = CreateEngine();
        engine.Start();
        engine.NextBeat(); // beat-2
        engine.AdvanceStep(); // beat-2 step 1
        clock.Advance(TimeSpan.FromMinutes(16));
        engine.AdvanceStep(); // beat-3
        engine.NextBeat(); // finished

        var snapshot = engine.GetSnapshot();

        Assert.True(snapshot.IsFinished);
        Assert.Equal(2, snapshot.BeatIndex);
        Assert.Equal("beat-3", snapshot.CurrentBeat.Id);
        Assert.Equal(TimeSpan.Zero, snapshot.Remaining);
        Assert.Equal(1, snapshot.OverallProgress);
        Assert.Equal(3, snapshot.CompletedBeatTimings.Count);
    }

    [Fact]
    public void FinishedSnapshot_ElapsedDoesNotAdvanceFurther()
    {
        var (engine, clock) = CreateEngine();
        engine.Start();
        engine.NextBeat();
        engine.AdvanceStep();
        engine.AdvanceStep();
        engine.NextBeat();

        var elapsedAtFinish = engine.GetSnapshot().Elapsed;
        clock.Advance(TimeSpan.FromMinutes(5));

        Assert.Equal(elapsedAtFinish, engine.GetSnapshot().Elapsed);
    }

    [Fact]
    public void CompletedBeatTimings_ActualsMatchClockAdvances()
    {
        var (engine, clock) = CreateEngine();
        engine.Start();
        clock.Advance(TimeSpan.FromMinutes(6));
        engine.NextBeat();
        clock.Advance(TimeSpan.FromMinutes(9));
        engine.AdvanceStep();
        clock.Advance(TimeSpan.FromMinutes(2));
        engine.AdvanceStep();

        var timings = engine.GetSnapshot().CompletedBeatTimings;

        Assert.Equal(2, timings.Count);
        Assert.Equal(TimeSpan.FromMinutes(6), timings[0].Actual);
        Assert.Equal(TimeSpan.FromMinutes(11), timings[1].Actual);
    }

    [Fact]
    public void Pace_IsBehind_WhenElapsedExceedsToleranceAtPosition()
    {
        var (engine, clock) = CreateEngine();
        engine.Start();
        clock.Advance(TimeSpan.FromMinutes(10));

        var snapshot = engine.GetSnapshot();

        Assert.Equal(PaceStatus.Behind, snapshot.Pace);
    }

    [Fact]
    public void Pace_IsOnPaceOrAhead_WhenElapsedUnderPlan()
    {
        var (engine, clock) = CreateEngine();
        engine.Start();
        clock.Advance(TimeSpan.FromMinutes(2));

        var snapshot = engine.GetSnapshot();

        Assert.Equal(PaceStatus.OnPaceOrAhead, snapshot.Pace);
    }
}
