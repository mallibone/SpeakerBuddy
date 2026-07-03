namespace StageBuddy.Core.Live;

public static class PaceCalculator
{
    public static readonly TimeSpan DefaultTolerance = TimeSpan.FromMinutes(1);

    public static PaceStatus Compute(TimeSpan elapsed, TimeSpan cumulativePlannedAtPosition, TimeSpan? tolerance = null)
    {
        var effectiveTolerance = tolerance ?? DefaultTolerance;

        if (elapsed <= cumulativePlannedAtPosition)
            return PaceStatus.OnPaceOrAhead;

        if (elapsed <= cumulativePlannedAtPosition + effectiveTolerance)
            return PaceStatus.WithinTolerance;

        return PaceStatus.Behind;
    }
}
