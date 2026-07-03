using StageBuddy.Core.Sync;

namespace StageBuddy.Core.Tests;

public class RehearsalAwareSnippetSenderTests
{
    private CancellationToken Ct => TestContext.Current.CancellationToken;

    private sealed class FakeSnippetSender : ISnippetSender
    {
        public ConnectionState State { get; private set; } = ConnectionState.Disconnected;
        public event EventHandler<ConnectionState>? StateChanged;
        public int StartPairingCalls { get; private set; }
        public int StopPairingCalls { get; private set; }
        public SendResult NextResult { get; set; } = SendResult.Sent;
        public Snippet? LastSentSnippet { get; private set; }

        public void StartPairing() => StartPairingCalls++;
        public void StopPairing() => StopPairingCalls++;

        public void SetState(ConnectionState state)
        {
            State = state;
            StateChanged?.Invoke(this, state);
        }

        public Task<SendResult> SendAsync(Snippet snippet, CancellationToken ct = default)
        {
            LastSentSnippet = snippet;
            return Task.FromResult(NextResult);
        }
    }

    private static Snippet SampleSnippet() => new("dotnet build", "bash", "Talk", "step-1");

    [Fact]
    public async Task RehearsalModeOff_RoutesSendAsyncToLiveSender()
    {
        var live = new FakeSnippetSender();
        var rehearsal = new FakeSnippetSender();
        var toggle = new RehearsalModeToggle();
        var sut = new RehearsalAwareSnippetSender(live, rehearsal, toggle);

        await sut.SendAsync(SampleSnippet(), Ct);

        Assert.NotNull(live.LastSentSnippet);
        Assert.Null(rehearsal.LastSentSnippet);
    }

    [Fact]
    public async Task RehearsalModeOn_RoutesSendAsyncToRehearsalSender()
    {
        var live = new FakeSnippetSender();
        var rehearsal = new FakeSnippetSender();
        var toggle = new RehearsalModeToggle();
        toggle.SetRehearsalMode(true);
        var sut = new RehearsalAwareSnippetSender(live, rehearsal, toggle);

        await sut.SendAsync(SampleSnippet(), Ct);

        Assert.NotNull(rehearsal.LastSentSnippet);
        Assert.Null(live.LastSentSnippet);
    }

    [Fact]
    public void State_ReflectsTheActiveSender()
    {
        var live = new FakeSnippetSender();
        var rehearsal = new FakeSnippetSender();
        var toggle = new RehearsalModeToggle();
        var sut = new RehearsalAwareSnippetSender(live, rehearsal, toggle);

        live.SetState(ConnectionState.Connected);
        rehearsal.SetState(ConnectionState.Discovering);

        Assert.Equal(ConnectionState.Connected, sut.State);

        toggle.SetRehearsalMode(true);

        Assert.Equal(ConnectionState.Discovering, sut.State);
    }

    [Fact]
    public void StartPairingAndStopPairing_AlwaysForwardToLiveSender()
    {
        var live = new FakeSnippetSender();
        var rehearsal = new FakeSnippetSender();
        var toggle = new RehearsalModeToggle();
        toggle.SetRehearsalMode(true);
        var sut = new RehearsalAwareSnippetSender(live, rehearsal, toggle);

        sut.StartPairing();
        sut.StopPairing();

        Assert.Equal(1, live.StartPairingCalls);
        Assert.Equal(1, live.StopPairingCalls);
        Assert.Equal(0, rehearsal.StartPairingCalls);
        Assert.Equal(0, rehearsal.StopPairingCalls);
    }

    [Fact]
    public void StateChanged_OnlyForwardsEventsFromTheActiveSender()
    {
        var live = new FakeSnippetSender();
        var rehearsal = new FakeSnippetSender();
        var toggle = new RehearsalModeToggle();
        var sut = new RehearsalAwareSnippetSender(live, rehearsal, toggle);

        var observed = new List<ConnectionState>();
        sut.StateChanged += (_, state) => observed.Add(state);

        rehearsal.SetState(ConnectionState.Connected);
        live.SetState(ConnectionState.Connecting);

        Assert.Equal([ConnectionState.Connecting], observed);
    }

    [Fact]
    public void TogglingRehearsalMode_RaisesStateChangedForTheNewlyActiveSender()
    {
        var live = new FakeSnippetSender();
        var rehearsal = new FakeSnippetSender();
        var toggle = new RehearsalModeToggle();
        live.SetState(ConnectionState.Connected);
        rehearsal.SetState(ConnectionState.Discovering);
        var sut = new RehearsalAwareSnippetSender(live, rehearsal, toggle);

        var observed = new List<ConnectionState>();
        sut.StateChanged += (_, state) => observed.Add(state);

        toggle.SetRehearsalMode(true);

        Assert.Equal([ConnectionState.Discovering], observed);
    }

    [Fact]
    public void LiveSender_ExposesTheUnderlyingLiveSenderForUnwrapping()
    {
        var live = new FakeSnippetSender();
        var rehearsal = new FakeSnippetSender();
        var toggle = new RehearsalModeToggle();
        var sut = new RehearsalAwareSnippetSender(live, rehearsal, toggle);

        Assert.Same(live, sut.LiveSender);
    }
}
