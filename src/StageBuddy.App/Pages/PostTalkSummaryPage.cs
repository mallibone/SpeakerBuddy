using StageBuddy.Core.Live;
using StageBuddy.Core.Models;
using StageBuddy.Core.Storage;

namespace StageBuddy.App.Pages;

class PostTalkSummaryPageProps
{
    public string TalkId { get; set; } = string.Empty;
    public string TalkTitle { get; set; } = string.Empty;
    public DateTimeOffset StartedAt { get; set; }
    public IReadOnlyList<Beat> Timeline { get; set; } = [];
    public IReadOnlyList<BeatTiming> BeatTimings { get; set; } = [];
}

class PostTalkSummaryPageState;

class PostTalkSummaryPage : Component<PostTalkSummaryPageState, PostTalkSummaryPageProps>
{
    private ITalkRunLogRepository RunLogRepository => Services.GetRequiredService<ITalkRunLogRepository>();

    private TimeSpan TotalPlanned => TimeSpan.FromTicks(Props.BeatTimings.Sum(b => b.Planned.Ticks));
    private TimeSpan TotalActual => TimeSpan.FromTicks(Props.BeatTimings.Sum(b => b.Actual.Ticks));

    protected override void OnMounted()
    {
        base.OnMounted();
        _ = PersistRunLogAsync();
    }

    private Task PersistRunLogAsync()
    {
        var runLog = new TalkRunLog(Props.TalkId, Props.StartedAt, Props.BeatTimings, TotalPlanned, TotalActual);
        return RunLogRepository.SaveAsync(runLog);
    }

    public override VisualNode Render()
        => ContentPage(
            VStack(
                Label(Props.TalkTitle)
                    .FontSize(20)
                    .FontFamily("OpenSansSemibold"),

                RenderTotalDelta(),

                CollectionView()
                    .ItemsSource(Props.BeatTimings, RenderRow)
                    .VFill(),

                Button("Done")
                    .OnClicked(Done)
                    .ImageSource(Icons.Glyph(Icons.Check, ApplicationTheme.OnAccent, 18))
                    .HeightRequest(52)
                    .AutomationId("SummaryDoneButton")
            )
            .Spacing(12)
            .Padding(16)
        )
        .Title("Talk Summary");

    private VisualNode RenderTotalDelta()
    {
        var delta = TotalActual - TotalPlanned;
        var deltaColor = delta > TimeSpan.Zero ? ApplicationTheme.PaceBehind : ApplicationTheme.PaceOnTrack;

        return Border(
            VStack(
                Label(delta switch
                    {
                        { Ticks: 0 } => "On time",
                        { Ticks: > 0 } => $"+{FormatTime(delta)} over",
                        _ => $"-{FormatTime(delta.Negate())} under"
                    })
                    .FontSize(28)
                    .FontFamily("OpenSansSemibold")
                    .TextColor(deltaColor)
                    .HCenter()
                    .AutomationId("SummaryTotalDelta"),
                Label($"{FormatTime(TotalActual)} actual · {FormatTime(TotalPlanned)} planned")
                    .TextColor(ApplicationTheme.TextSecondary)
                    .HCenter()
            )
            .Spacing(4)
        )
        .BackgroundColor(ApplicationTheme.Card)
        .Padding(16);
    }

    private VisualNode RenderRow(BeatTiming timing)
    {
        var title = Props.Timeline.FirstOrDefault(b => b.Id == timing.BeatId)?.Title ?? timing.BeatId;
        var delta = timing.Actual - timing.Planned;
        var deltaColor = delta > TimeSpan.Zero ? ApplicationTheme.PaceBehind : ApplicationTheme.PaceOnTrack;

        return Grid("auto", "*,auto",
            VStack(
                Label(title)
                    .FontFamily("OpenSansSemibold"),
                Label($"{FormatTime(timing.Actual)} actual · {FormatTime(timing.Planned)} planned")
                    .FontSize(13)
                    .TextColor(ApplicationTheme.TextSecondary)
            )
            .Spacing(2)
            .GridColumn(0),

            Label(FormatDelta(delta))
                .FontFamily("OpenSansSemibold")
                .TextColor(deltaColor)
                .VCenter()
                .GridColumn(1)
        )
        .Padding(0, 10);
    }

    private async void Done() => await Navigation!.PopToRootAsync();

    private static string FormatTime(TimeSpan span) => span.ToString(@"mm\:ss");

    private static string FormatDelta(TimeSpan delta) => delta switch
    {
        { Ticks: 0 } => "on time",
        { Ticks: > 0 } => $"+{FormatTime(delta)}",
        _ => $"-{FormatTime(delta.Negate())}"
    };
}
