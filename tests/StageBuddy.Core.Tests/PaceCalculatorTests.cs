using StageBuddy.Core.Live;

namespace StageBuddy.Core.Tests;

public class PaceCalculatorTests
{
    private static readonly TimeSpan Planned = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan Tolerance = TimeSpan.FromMinutes(1);

    [Fact]
    public void AheadOfSchedule_IsOnPaceOrAhead()
    {
        var status = PaceCalculator.Compute(TimeSpan.FromMinutes(5), Planned, Tolerance);

        Assert.Equal(PaceStatus.OnPaceOrAhead, status);
    }

    [Fact]
    public void ExactlyOnPlan_IsOnPaceOrAhead()
    {
        var status = PaceCalculator.Compute(Planned, Planned, Tolerance);

        Assert.Equal(PaceStatus.OnPaceOrAhead, status);
    }

    [Fact]
    public void JustPastPlan_IsWithinTolerance()
    {
        var status = PaceCalculator.Compute(Planned + TimeSpan.FromMilliseconds(1), Planned, Tolerance);

        Assert.Equal(PaceStatus.WithinTolerance, status);
    }

    [Fact]
    public void ExactlyAtTolerance_IsWithinTolerance()
    {
        var status = PaceCalculator.Compute(Planned + Tolerance, Planned, Tolerance);

        Assert.Equal(PaceStatus.WithinTolerance, status);
    }

    [Fact]
    public void JustPastTolerance_IsBehind()
    {
        var status = PaceCalculator.Compute(Planned + Tolerance + TimeSpan.FromMilliseconds(1), Planned, Tolerance);

        Assert.Equal(PaceStatus.Behind, status);
    }

    [Fact]
    public void WellPastTolerance_IsBehind()
    {
        var status = PaceCalculator.Compute(Planned + TimeSpan.FromMinutes(10), Planned, Tolerance);

        Assert.Equal(PaceStatus.Behind, status);
    }

    [Fact]
    public void OmittedTolerance_DefaultsToOneMinute()
    {
        var withinDefault = PaceCalculator.Compute(Planned + TimeSpan.FromMinutes(1), Planned);
        var beyondDefault = PaceCalculator.Compute(Planned + TimeSpan.FromMinutes(1) + TimeSpan.FromMilliseconds(1), Planned);

        Assert.Equal(PaceStatus.WithinTolerance, withinDefault);
        Assert.Equal(PaceStatus.Behind, beyondDefault);
    }
}
