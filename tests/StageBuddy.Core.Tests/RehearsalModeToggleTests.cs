using StageBuddy.Core.Sync;

namespace StageBuddy.Core.Tests;

public class RehearsalModeToggleTests
{
    [Fact]
    public void DefaultsToDisabled()
    {
        var toggle = new RehearsalModeToggle();

        Assert.False(toggle.IsRehearsalMode);
    }

    [Fact]
    public void SetRehearsalMode_UpdatesStateAndRaisesChanged()
    {
        var toggle = new RehearsalModeToggle();
        bool? raised = null;
        toggle.Changed += (_, enabled) => raised = enabled;

        toggle.SetRehearsalMode(true);

        Assert.True(toggle.IsRehearsalMode);
        Assert.True(raised);
    }

    [Fact]
    public void SetRehearsalMode_SameValue_DoesNotRaiseChanged()
    {
        var toggle = new RehearsalModeToggle();
        var raiseCount = 0;
        toggle.Changed += (_, _) => raiseCount++;

        toggle.SetRehearsalMode(false);

        Assert.Equal(0, raiseCount);
    }
}
