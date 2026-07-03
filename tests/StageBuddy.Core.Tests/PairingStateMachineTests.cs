using Microsoft.Extensions.Time.Testing;
using StageBuddy.Core.Sync;

namespace StageBuddy.Core.Tests;

public class PairingStateMachineTests
{
    private static (PairingStateMachine Machine, FakeTimeProvider Clock) Create()
    {
        var clock = new FakeTimeProvider();
        return (new PairingStateMachine(clock), clock);
    }

    private static void MoveTo(PairingStateMachine machine, ConnectionState state)
    {
        switch (state)
        {
            case ConnectionState.Disconnected:
                return;
            case ConnectionState.Discovering:
                machine.Start();
                return;
            case ConnectionState.Connecting:
                machine.Start();
                machine.PeerFound();
                return;
            case ConnectionState.Connected:
                machine.Start();
                machine.PeerFound();
                machine.SessionConnected();
                return;
        }
    }

    [Fact]
    public void InitialState_IsDisconnected()
    {
        var (machine, _) = Create();

        Assert.Equal(ConnectionState.Disconnected, machine.State);
    }

    [Fact]
    public void Start_FromDisconnected_MovesToDiscovering()
    {
        var (machine, _) = Create();

        machine.Start();

        Assert.Equal(ConnectionState.Discovering, machine.State);
    }

    [Fact]
    public void Start_WhileAlreadyActive_IsNoOp()
    {
        var (machine, _) = Create();
        machine.Start();
        machine.PeerFound();

        machine.Start();

        Assert.Equal(ConnectionState.Connecting, machine.State);
    }

    [Fact]
    public void PeerFound_FromDiscovering_MovesToConnecting()
    {
        var (machine, _) = Create();
        machine.Start();

        machine.PeerFound();

        Assert.Equal(ConnectionState.Connecting, machine.State);
    }

    [Theory]
    [InlineData(ConnectionState.Disconnected)]
    [InlineData(ConnectionState.Connecting)]
    [InlineData(ConnectionState.Connected)]
    public void PeerFound_WhenNotDiscovering_IsNoOp(ConnectionState state)
    {
        var (machine, _) = Create();
        MoveTo(machine, state);
        var before = machine.State;

        machine.PeerFound();

        Assert.Equal(before, machine.State);
    }

    [Fact]
    public void SessionConnected_FromConnecting_MovesToConnected()
    {
        var (machine, _) = Create();
        machine.Start();
        machine.PeerFound();

        machine.SessionConnected();

        Assert.Equal(ConnectionState.Connected, machine.State);
    }

    [Theory]
    [InlineData(ConnectionState.Disconnected)]
    [InlineData(ConnectionState.Discovering)]
    [InlineData(ConnectionState.Connected)]
    public void SessionConnected_WhenNotConnecting_IsNoOp(ConnectionState state)
    {
        var (machine, _) = Create();
        MoveTo(machine, state);
        var before = machine.State;

        machine.SessionConnected();

        Assert.Equal(before, machine.State);
    }

    [Fact]
    public void SessionDisconnected_FromConnected_SchedulesRetry_BackToDiscovering()
    {
        var (machine, _) = Create();
        MoveTo(machine, ConnectionState.Connected);

        machine.SessionDisconnected();

        Assert.Equal(ConnectionState.Discovering, machine.State);
        Assert.Equal(TimeSpan.FromSeconds(1), machine.LastBackoffDelay);
    }

    [Fact]
    public void SessionDisconnected_FromConnecting_SchedulesRetry()
    {
        var (machine, _) = Create();
        machine.Start();
        machine.PeerFound();

        machine.SessionDisconnected();

        Assert.Equal(ConnectionState.Discovering, machine.State);
        Assert.Equal(TimeSpan.FromSeconds(1), machine.LastBackoffDelay);
    }

    [Theory]
    [InlineData(ConnectionState.Disconnected)]
    [InlineData(ConnectionState.Discovering)]
    public void SessionDisconnected_WhenNoActiveSession_IsNoOp(ConnectionState state)
    {
        var (machine, _) = Create();
        MoveTo(machine, state);
        var before = machine.State;

        machine.SessionDisconnected();

        Assert.Equal(before, machine.State);
        Assert.Null(machine.LastBackoffDelay);
    }

    [Fact]
    public void PeerLost_FromConnecting_SchedulesRetry()
    {
        var (machine, _) = Create();
        machine.Start();
        machine.PeerFound();

        machine.PeerLost();

        Assert.Equal(ConnectionState.Discovering, machine.State);
        Assert.Equal(TimeSpan.FromSeconds(1), machine.LastBackoffDelay);
    }

    [Fact]
    public void PeerLost_FromConnected_SchedulesRetry()
    {
        var (machine, _) = Create();
        MoveTo(machine, ConnectionState.Connected);

        machine.PeerLost();

        Assert.Equal(ConnectionState.Discovering, machine.State);
        Assert.Equal(TimeSpan.FromSeconds(1), machine.LastBackoffDelay);
    }

    [Fact]
    public void PeerLost_WhileDiscovering_IsNoOp()
    {
        var (machine, _) = Create();
        machine.Start();

        machine.PeerLost();

        Assert.Equal(ConnectionState.Discovering, machine.State);
        Assert.Null(machine.LastBackoffDelay);
    }

    [Fact]
    public void BackoffSequence_Is1_2_4_5_5_Repeating()
    {
        var (machine, clock) = Create();
        machine.Start();

        var observed = new List<TimeSpan>();
        for (var i = 0; i < 5; i++)
        {
            machine.PeerFound();
            machine.SessionDisconnected();
            observed.Add(machine.LastBackoffDelay!.Value);
            clock.Advance(observed[^1]);
        }

        Assert.Equal(
        [
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(4),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(5)
        ], observed);
    }

    [Fact]
    public void RetriesForever_UntilStop()
    {
        var (machine, clock) = Create();
        machine.Start();

        for (var i = 0; i < 10; i++)
        {
            machine.PeerFound();
            machine.SessionDisconnected();
            clock.Advance(machine.LastBackoffDelay!.Value);
        }

        Assert.Equal(ConnectionState.Discovering, machine.State);

        machine.Stop();

        Assert.Equal(ConnectionState.Disconnected, machine.State);

        clock.Advance(TimeSpan.FromSeconds(10));
        machine.PeerFound();

        Assert.Equal(ConnectionState.Disconnected, machine.State);
    }

    [Fact]
    public void PermissionDenied_IsTerminal_UntilStart()
    {
        var (machine, clock) = Create();
        machine.Start();
        machine.PeerFound();

        machine.PermissionDenied();

        Assert.Equal(ConnectionState.Disconnected, machine.State);

        clock.Advance(TimeSpan.FromSeconds(10));
        machine.PeerFound();

        Assert.Equal(ConnectionState.Disconnected, machine.State);

        machine.Start();

        Assert.Equal(ConnectionState.Discovering, machine.State);
    }

    [Fact]
    public void PermissionDenied_ResetsBackoffCounter()
    {
        var (machine, clock) = Create();
        machine.Start();
        machine.PeerFound();
        machine.SessionDisconnected();
        clock.Advance(TimeSpan.FromSeconds(1));

        machine.PeerFound();
        machine.PermissionDenied();
        machine.Start();
        machine.PeerFound();
        machine.SessionDisconnected();

        Assert.Equal(TimeSpan.FromSeconds(1), machine.LastBackoffDelay);
    }

    [Fact]
    public void SuccessfulConnection_ResetsBackoffCounter()
    {
        var (machine, clock) = Create();
        machine.Start();
        machine.PeerFound();
        machine.SessionDisconnected();
        clock.Advance(TimeSpan.FromSeconds(1));
        machine.PeerFound();
        machine.SessionConnected();

        machine.SessionDisconnected();

        Assert.Equal(TimeSpan.FromSeconds(1), machine.LastBackoffDelay);
    }

    [Fact]
    public void PeerFound_DuringBackoffWindow_IsIgnored()
    {
        var (machine, clock) = Create();
        machine.Start();
        machine.PeerFound();
        machine.SessionDisconnected();

        machine.PeerFound();

        Assert.Equal(ConnectionState.Discovering, machine.State);

        clock.Advance(TimeSpan.FromSeconds(1));
        machine.PeerFound();

        Assert.Equal(ConnectionState.Connecting, machine.State);
    }

    [Fact]
    public void StateChanged_IsRaisedOnEveryTransition()
    {
        var (machine, _) = Create();
        var transitions = new List<ConnectionState>();
        machine.StateChanged += (_, s) => transitions.Add(s);

        machine.Start();
        machine.PeerFound();
        machine.SessionConnected();

        Assert.Equal(
        [
            ConnectionState.Discovering,
            ConnectionState.Connecting,
            ConnectionState.Connected
        ], transitions);
    }

    [Fact]
    public void StateChanged_NotRaised_OnNoOpTransition()
    {
        var (machine, _) = Create();
        var raised = 0;
        machine.StateChanged += (_, _) => raised++;

        machine.SessionConnected();

        Assert.Equal(0, raised);
    }
}
