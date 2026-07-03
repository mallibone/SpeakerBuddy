using StageBuddy.Core.Storage;

namespace StageBuddy.App.Pages;

class LiveTalkPageProps
{
    public string TalkId { get; set; } = string.Empty;
}

class LiveTalkPageState
{
    public string Title { get; set; } = string.Empty;
    public bool IsLoading { get; set; } = true;
}

class LiveTalkPage : Component<LiveTalkPageState, LiveTalkPageProps>
{
    private ITalkRepository Repository => Services.GetRequiredService<ITalkRepository>();

    protected override void OnMounted()
    {
        base.OnMounted();
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        var talk = await Repository.LoadAsync(Props.TalkId);
        SetState(s =>
        {
            s.Title = talk?.Session.Title ?? "Talk not found";
            s.IsLoading = false;
        });
    }

    public override VisualNode Render()
        => ContentPage(
            State.IsLoading
                ? ActivityIndicator().IsRunning(true).VCenter().HCenter()
                : Label(State.Title)
                    .FontSize(20)
                    .HCenter()
                    .VCenter()
        )
        .Title("Live");
}
