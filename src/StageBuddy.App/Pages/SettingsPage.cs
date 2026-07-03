namespace StageBuddy.App.Pages;

class SettingsPage : Component
{
    public override VisualNode Render()
        => ContentPage(
            VStack(
                Label("Local clipboard mode")
                    .FontSize(18)
                    .FontAttributes(FontAttributes.Bold),
                Label("Copy sends each snippet straight to this device's clipboard. Pairing with a Mac helper app arrives in a later update.")
                    .TextColor(Colors.Gray)
            )
            .Spacing(8)
            .Padding(16)
        )
        .Title("Settings");
}
