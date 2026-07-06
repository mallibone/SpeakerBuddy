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
        .Title("Stage Buddy")
        // Reload whenever the page reappears: a pop back from a finished run (or an import)
        // must pick up new run logs/talks — OnMounted alone leaves the list stale.
        .OnAppearing(() => _ = ReloadAsync());

    private VisualNode RenderHeader()
        => Grid("auto", "*,auto,auto",
            Button("Import")
                .OnClicked(NavigateToImport)
                .ImageSource(Icons.Glyph(Icons.Import, ApplicationTheme.OnAccent, 18))
                .AutomationId("LibraryImportButton")
                .GridColumn(1),
            Button("Settings")
                .OnClicked(NavigateToSettings)
                .ImageSource(Icons.Glyph(Icons.Settings, ApplicationTheme.Accent, 18))
                .ThemeKey("Secondary")
                .AutomationId("LibrarySettingsButton")
                .GridColumn(2)
        )
        .ColumnSpacing(10)
        .Padding(16, 10);

    private static VisualNode RenderEmptyState()
        => VStack(
            Label(Icons.Mic)
                .FontFamily(Icons.FontFamily)
                .FontSize(52)
                .TextColor(ApplicationTheme.TextSecondary)
                .HCenter(),
            Label("No talks yet")
                .FontSize(20)
                .FontFamily("OpenSansSemibold")
                .HCenter()
                .AutomationId("LibraryEmptyState"),
            Label("Tap Import to paste a talk's JSON")
                .TextColor(ApplicationTheme.TextSecondary)
                .HCenter()
        )
        .VCenter()
        .VFill()
        .Spacing(10);

    private VisualNode RenderList()
        => CollectionView()
            .ItemsSource(State.Talks, RenderTalkRow)
            .VFill();

    private VisualNode RenderTalkRow(TalkSummary talk)
        => Border(
            Grid("auto", "*,auto",
                VStack(
                    Label(talk.Title)
                        .FontSize(17)
                        .FontFamily("OpenSansSemibold"),
                    Label($"{talk.PlannedDurationMinutes} min · {talk.BeatCount} beats")
                        .FontSize(13)
                        .TextColor(ApplicationTheme.TextSecondary),
                    RenderLastRunDelta(talk.TalkId)
                )
                .Spacing(4)
                .GridColumn(0),

                Label(Icons.Chevron)
                    .FontFamily(Icons.FontFamily)
                    .FontSize(18)
                    .TextColor(ApplicationTheme.TextSecondary)
                    .VCenter()
                    .GridColumn(1)
            )
        )
        .BackgroundColor(ApplicationTheme.Card)
        .Padding(16, 12)
        .Margin(16, 6)
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

        return HStack(
            Label(Icons.History)
                .FontFamily(Icons.FontFamily)
                .FontSize(12)
                .TextColor(delta > TimeSpan.Zero ? ApplicationTheme.PaceBehind : ApplicationTheme.PaceOnTrack)
                .VCenter(),
            Label($"Last run: {description}")
                .FontSize(12)
                .TextColor(delta > TimeSpan.Zero ? ApplicationTheme.PaceBehind : ApplicationTheme.PaceOnTrack)
                .VCenter()
                .AutomationId($"TalkLastRun-{talkId}")
        )
        .Spacing(4);
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
