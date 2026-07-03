namespace StageBuddy.Core.Sync;

public sealed class PairingStateMachine(TimeProvider clock)
{
    private static readonly TimeSpan[] BackoffSteps =
    [
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(4)
    ];

    private static readonly TimeSpan MaxBackoff = TimeSpan.FromSeconds(5);

    private int _consecutiveFailures;
    private DateTimeOffset _retryNotBefore = DateTimeOffset.MinValue;

    public ConnectionState State { get; private set; } = ConnectionState.Disconnected;
    public TimeSpan? LastBackoffDelay { get; private set; }
    public event EventHandler<ConnectionState>? StateChanged;

    public void Start()
    {
        if (State != ConnectionState.Disconnected)
            return;

        ResetBackoff();
        TransitionTo(ConnectionState.Discovering);
    }

    public void Stop()
    {
        ResetBackoff();
        TransitionTo(ConnectionState.Disconnected);
    }

    public void PermissionDenied()
    {
        ResetBackoff();
        TransitionTo(ConnectionState.Disconnected);
    }

    public void PeerFound()
    {
        if (State != ConnectionState.Discovering)
            return;

        if (clock.GetUtcNow() < _retryNotBefore)
            return;

        TransitionTo(ConnectionState.Connecting);
    }

    public void PeerLost()
    {
        if (State is ConnectionState.Connecting or ConnectionState.Connected)
            ScheduleRetry();
    }

    public void SessionConnected()
    {
        if (State != ConnectionState.Connecting)
            return;

        _consecutiveFailures = 0;
        LastBackoffDelay = null;
        TransitionTo(ConnectionState.Connected);
    }

    public void SessionDisconnected()
    {
        if (State is ConnectionState.Connecting or ConnectionState.Connected)
            ScheduleRetry();
    }

    private void ScheduleRetry()
    {
        var delay = _consecutiveFailures < BackoffSteps.Length ? BackoffSteps[_consecutiveFailures] : MaxBackoff;
        _consecutiveFailures++;
        LastBackoffDelay = delay;
        _retryNotBefore = clock.GetUtcNow() + delay;
        TransitionTo(ConnectionState.Discovering);
    }

    private void ResetBackoff()
    {
        _consecutiveFailures = 0;
        LastBackoffDelay = null;
        _retryNotBefore = DateTimeOffset.MinValue;
    }

    private void TransitionTo(ConnectionState newState)
    {
        if (State == newState)
            return;

        State = newState;
        StateChanged?.Invoke(this, newState);
    }
}
