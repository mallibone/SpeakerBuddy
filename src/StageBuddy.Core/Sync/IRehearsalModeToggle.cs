namespace StageBuddy.Core.Sync;

public interface IRehearsalModeToggle
{
    bool IsRehearsalMode { get; }
    event EventHandler<bool>? Changed;
    void SetRehearsalMode(bool enabled);
}
