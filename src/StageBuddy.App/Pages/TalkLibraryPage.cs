using StageBuddy.Core.Models;
using StageBuddy.Core.Storage;

namespace StageBuddy.App.Pages;

class TalkLibraryPageState
{
    public IReadOnlyList<TalkSummary> Talks { get; set; } = [];
    public IReadOnlyDictionary<string, TalkRunLog> LastRuns { get; set; } = new Dictionary<string, TalkRunLog>();
    public bool IsLoading { get; set; } = true;
}

class TalkLibraryPage : Component<TalkLibraryPageState>
{
    private ITalkRepository Repository => Services.GetRequiredService<ITalkRepository>();
    private ITalkRunLogRepository RunLogRepository => Services.GetRequiredService<ITalkRunLogRepository>();

    protected override void OnMounted()
    {
        base.OnMounted();
        _ = ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        var talks = await Repository.ListAsync();

        var lastRuns = new Dictionary<string, TalkRunLog>();
        foreach (var talk in talks)
        {
            var lastRun = await RunLogRepository.GetLatestAsync(talk.TalkId);
            if (lastRun is not null)
                lastRuns[talk.TalkId] = lastRun;
        }

        SetState(s =>
        {
            s.Talks = talks;
            s.LastRuns = lastRuns;
            s.IsLoading = false;
        });
    }

    public override VisualNode Render()
        => ContentPage(
            VStack(
                RenderHeader(),

                State.IsLoading
                    ? ActivityIndicator().IsRunning(true).VCenter().HCenter().VFill()
                    : State.Talks.Count == 0
                        ? RenderEmptyState()
                        : RenderList()
            )
        )
        .Title("Stage Buddy");

    private VisualNode RenderHeader()
        => HStack(
            Button("Import").OnClicked(NavigateToImport).AutomationId("LibraryImportButton"),
            Button("Settings").OnClicked(NavigateToSettings).AutomationId("LibrarySettingsButton")
        )
        .Spacing(12)
        .Padding(16, 8);

    private static VisualNode RenderEmptyState()
        => VStack(
            Label("No talks yet")
                .FontSize(20)
                .HCenter()
                .AutomationId("LibraryEmptyState"),
            Label("Tap Import to paste a talk's JSON")
                .TextColor(Colors.Gray)
                .HCenter()
        )
        .VCenter()
        .VFill()
        .Spacing(8);

    private VisualNode RenderList()
        => CollectionView()
            .ItemsSource(State.Talks, RenderTalkRow)
            .VFill();

    private VisualNode RenderTalkRow(TalkSummary talk)
        => VStack(
            Label(talk.Title)
                .FontSize(18)
                .FontAttributes(FontAttributes.Bold),
            Label($"{talk.PlannedDurationMinutes} min · {talk.BeatCount} beats")
                .FontSize(14)
                .TextColor(Colors.Gray),
            RenderLastRunDelta(talk.TalkId)
        )
        .Padding(16, 12)
        .OnTapped(() => NavigateToLiveTalk(talk))
        .AutomationId($"TalkRow-{talk.TalkId}");

    private VisualNode? RenderLastRunDelta(string talkId)
    {
        if (!State.LastRuns.TryGetValue(talkId, out var lastRun))
            return null;

        var delta = lastRun.TotalActual - lastRun.TotalPlanned;
        var description = delta switch
        {
            { Ticks: 0 } => "on time",
            { Ticks: > 0 } => $"+{delta:mm\\:ss} over",
            _ => $"-{delta.Negate():mm\\:ss} under"
        };

        return Label($"Last run: {description}")
            .FontSize(12)
            .TextColor(delta > TimeSpan.Zero ? Colors.Red : Colors.Green)
            .AutomationId($"TalkLastRun-{talkId}");
    }

    private async void NavigateToImport()
    {
        await Navigation.PushAsync<ImportPage, ImportPageProps>(props =>
        {
            props.OnImported = () => _ = ReloadAsync();
        });
    }

    private async void NavigateToSettings()
    {
        await Navigation.PushAsync<SettingsPage>();
    }

    private async void NavigateToLiveTalk(TalkSummary talk)
    {
        await Navigation.PushAsync<LiveTalkPage, LiveTalkPageProps>(props =>
        {
            props.TalkId = talk.TalkId;
        });
    }
}
