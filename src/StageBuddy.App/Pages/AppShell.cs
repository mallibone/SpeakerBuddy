namespace StageBuddy.App.Pages;

class AppShell : Component
{
    public override VisualNode Render()
        => NavigationPage(new TalkLibraryPage());
}
