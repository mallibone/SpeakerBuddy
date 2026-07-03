namespace StageBuddy.App.Pages;

class AppShell : Component
{
    public override VisualNode Render()
        => new MauiReactor.NavigationPage
        {
            ContentPage(
                Label("Stage Buddy")
                    .FontSize(24)
                    .HCenter()
                    .VCenter()
            )
            .Title("Stage Buddy")
        };
}
