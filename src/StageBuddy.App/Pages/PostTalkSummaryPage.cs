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
                    .FontAttributes(FontAttributes.Bold),

                RenderTotalDelta(),

                CollectionView()
                    .ItemsSource(Props.BeatTimings, RenderRow)
                    .VFill(),

                Button("Done").OnClicked(Done)
            )
            .Spacing(12)
            .Padding(16)
        )
        .Title("Talk Summary");

    private VisualNode RenderTotalDelta()
    {
        var delta = TotalActual - TotalPlanned;
        return Label($"{FormatTime(TotalActual)} actual vs {FormatTime(TotalPlanned)} planned ({FormatDelta(delta)})")
            .FontAttributes(FontAttributes.Bold)
            .TextColor(delta > TimeSpan.Zero ? Colors.Red : Colors.Green);
    }

    private VisualNode RenderRow(BeatTiming timing)
    {
        var title = Props.Timeline.FirstOrDefault(b => b.Id == timing.BeatId)?.Title ?? timing.BeatId;
        var delta = timing.Actual - timing.Planned;

        return VStack(
            Label(title),
            Label($"{FormatTime(timing.Actual)} actual vs {FormatTime(timing.Planned)} planned ({FormatDelta(delta)})")
                .TextColor(delta > TimeSpan.Zero ? Colors.Red : Colors.Green)
        )
        .Padding(0, 8);
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
