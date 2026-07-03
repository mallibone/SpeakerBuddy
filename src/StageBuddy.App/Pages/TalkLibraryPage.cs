using StageBuddy.Core.Models;
using StageBuddy.Core.Storage;

namespace StageBuddy.App.Pages;

class TalkLibraryPageState
{
    public IReadOnlyList<TalkSummary> Talks { get; set; } = [];
    public bool IsLoading { get; set; } = true;
}

class TalkLibraryPage : Component<TalkLibraryPageState>
{
    private ITalkRepository Repository => Services.GetRequiredService<ITalkRepository>();

    protected override void OnMounted()
    {
        base.OnMounted();
        _ = ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        var talks = await Repository.ListAsync();
        SetState(s =>
        {
            s.Talks = talks;
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
            Button("Import").OnClicked(NavigateToImport),
            Button("Settings").OnClicked(NavigateToSettings)
        )
        .Spacing(12)
        .Padding(16, 8);

    private static VisualNode RenderEmptyState()
        => VStack(
            Label("No talks yet")
                .FontSize(20)
                .HCenter(),
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
                .TextColor(Colors.Gray)
        )
        .Padding(16, 12)
        .OnTapped(() => NavigateToLiveTalk(talk));

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
