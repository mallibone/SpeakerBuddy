using StageBuddy.Core.Sync;

namespace StageBuddy.App.Pages;

class AppShell : Component
{
    protected override void OnMounted()
    {
        base.OnMounted();
        Services.GetRequiredService<ISnippetSender>().StartPairing();
    }

    public override VisualNode Render()
        => NavigationPage(new TalkLibraryPage());
}
