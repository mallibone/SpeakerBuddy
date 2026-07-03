namespace StageBuddy.Core.Sync;

public sealed class RehearsalModeToggle : IRehearsalModeToggle
{
    public bool IsRehearsalMode { get; private set; }

    public event EventHandler<bool>? Changed;

    public void SetRehearsalMode(bool enabled)
    {
        if (IsRehearsalMode == enabled)
            return;

        IsRehearsalMode = enabled;
        Changed?.Invoke(this, enabled);
    }
}
