using StageBuddy.App.Components;
using StageBuddy.Core.Live;
using StageBuddy.Core.Models;
using StageBuddy.Core.Storage;
using StageBuddy.Core.Sync;

namespace StageBuddy.App.Pages;

class LiveTalkPageProps
{
    public string TalkId { get; set; } = string.Empty;
}

class LiveTalkPageState
{
    public bool IsLoading { get; set; } = true;
    public string Title { get; set; } = string.Empty;
    public TalkDocument? Talk { get; set; }
    public LiveTalkEngine? Engine { get; set; }
    public LiveTalkSnapshot? Snapshot { get; set; }
    public bool HasStarted { get; set; }
    public bool CopyFailed { get; set; }
    public bool HasNavigatedToSummary { get; set; }
}

class LiveTalkPage : Component<LiveTalkPageState, LiveTalkPageProps>
{
    private ITalkRepository Repository => Services.GetRequiredService<ITalkRepository>();
    private TimeProvider Clock => Services.GetRequiredService<TimeProvider>();
    private ISnippetSender Sender => Services.GetRequiredService<ISnippetSender>();
    private IRehearsalModeToggle RehearsalMode => Services.GetRequiredService<IRehearsalModeToggle>();

    private IDispatcherTimer? _timer;

    protected override void OnMounted()
    {
        base.OnMounted();
        DeviceDisplay.Current.KeepScreenOn = true;
        _ = LoadAsync();
    }

    protected override void OnWillUnmount()
    {
        _timer?.Stop();
        DeviceDisplay.Current.KeepScreenOn = false;
        base.OnWillUnmount();
    }

    private async Task LoadAsync()
    {
        var talk = await Repository.LoadAsync(Props.TalkId);
        if (talk is null)
        {
            SetState(s =>
            {
                s.IsLoading = false;
                s.Title = "Talk not found";
            });
            return;
        }

        var engine = new LiveTalkEngine(talk, Clock);

        SetState(s =>
        {
            s.IsLoading = false;
            s.Title = talk.Session.Title;
            s.Talk = talk;
            s.Engine = engine;
            s.Snapshot = engine.GetSnapshot();
        });

        _timer = Application.Current!.Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += (_, _) => ApplySnapshot(State.Engine?.GetSnapshot());
        _timer.Start();
    }

    private void ApplySnapshot(LiveTalkSnapshot? snapshot)
    {
        var shouldNavigateToSummary = !State.HasNavigatedToSummary && snapshot is { IsFinished: true };

        SetState(s =>
        {
            s.Snapshot = snapshot;
            if (shouldNavigateToSummary)
                s.HasNavigatedToSummary = true;
        });

        if (shouldNavigateToSummary)
        {
            _timer?.Stop();
            _ = NavigateToSummaryAsync(snapshot!);
        }
    }

    private async Task NavigateToSummaryAsync(LiveTalkSnapshot snapshot)
    {
        var engine = State.Engine;
        var talk = State.Talk;
        if (engine is null || talk is null)
            return;

        await Navigation!.PushAsync<PostTalkSummaryPage, PostTalkSummaryPageProps>(props =>
        {
            props.TalkId = Props.TalkId;
            props.TalkTitle = State.Title;
            props.StartedAt = engine.StartedAt ?? Clock.GetUtcNow();
            props.Timeline = talk.Timeline;
            props.BeatTimings = snapshot.CompletedBeatTimings;
        });
    }

    public override VisualNode Render()
        => ContentPage(
            State.IsLoading || State.Snapshot is null
                ? ActivityIndicator().IsRunning(true).VCenter().HCenter()
                : RenderLive()
        )
        .Title(State.Title);

    private VisualNode RenderLive()
        => VStack(
            RehearsalMode.IsRehearsalMode
                ? Label("REHEARSAL MODE")
                    .FontAttributes(FontAttributes.Bold)
                    .TextColor(Colors.White)
                    .BackgroundColor(Colors.DarkOrange)
                    .Padding(8, 4)
                    .HCenter()
                : null,

            new PacingBar().Snapshot(State.Snapshot!),

            State.Snapshot!.IsFinished
                ? RenderFinished()
                : RenderCurrentBeat(),

            !State.HasStarted
                ? Button("Start").OnClicked(StartTalk).HeightRequest(56)
                : null,

            Button("Back").OnClicked(GoBackAStep)
        )
        .Spacing(16)
        .Padding(16);

    private VisualNode RenderCurrentBeat()
        => State.Snapshot!.CurrentBeat.Type == BeatType.Demo
            ? new DemoStepView()
                .Step(State.Snapshot.CurrentStep!)
                .CopyFailed(State.CopyFailed)
                .IsSenderConnected(Sender.State == ConnectionState.Connected)
                .OnCopy(CopyCurrentStep)
            : new TalkBeatView()
                .Beat(State.Snapshot.CurrentBeat)
                .OnNext(NextBeat);

    private static VisualNode RenderFinished()
        => Label("Talk finished!")
            .FontSize(24)
            .HCenter()
            .VCenter();

    private void StartTalk()
    {
        State.Engine?.Start();
        SetState(s =>
        {
            s.HasStarted = true;
            s.Snapshot = s.Engine?.GetSnapshot();
        });
    }

    private void NextBeat()
    {
        State.Engine?.NextBeat();
        ApplySnapshot(State.Engine?.GetSnapshot());
    }

    private void GoBackAStep()
    {
        State.Engine?.Back();
        SetState(s =>
        {
            s.Snapshot = s.Engine?.GetSnapshot();
            s.CopyFailed = false;
        });
    }

    private async void CopyCurrentStep()
    {
        var engine = State.Engine;
        var snapshot = State.Snapshot;
        if (engine is null || snapshot?.CurrentStep is null)
            return;

        var snippet = new Snippet(
            snapshot.CurrentStep.Snippet ?? string.Empty,
            snapshot.CurrentStep.Language,
            State.Title,
            snapshot.CurrentStep.Id);

        var result = await Sender.SendAsync(snippet);

        if (result == SendResult.Sent)
        {
            engine.AdvanceStep();
            SetState(s => s.CopyFailed = false);
            ApplySnapshot(engine.GetSnapshot());
        }
        else
        {
            SetState(s => s.CopyFailed = true);
        }
    }
}
