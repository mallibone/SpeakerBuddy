using StageBuddy.Core.Models;

namespace StageBuddy.App.Components;

partial class TalkBeatView : Component
{
    [Prop]
    Beat? _beat;

    [Prop]
    Action? _onNext;

    public override VisualNode Render()
    {
        if (_beat is null)
            return VStack();

        return VStack(
            Label(_beat.Title)
                .FontSize(30)
                .FontFamily("OpenSansSemibold")
                .HorizontalTextAlignment(TextAlignment.Center)
                .HCenter(),

            HStack(
                Label(Icons.Timer)
                    .FontFamily(Icons.FontFamily)
                    .FontSize(16)
                    .TextColor(ApplicationTheme.TextSecondary)
                    .VCenter(),
                Label($"{_beat.PlannedDurationMinutes} min planned")
                    .TextColor(ApplicationTheme.TextSecondary)
                    .VCenter()
            )
            .Spacing(6)
            .HCenter(),

            Button("Next beat")
                .OnClicked(() => _onNext?.Invoke())
                .ImageSource(Icons.Glyph(Icons.Next, ApplicationTheme.OnAccent))
                .FontSize(18)
                .HeightRequest(64)
                .AutomationId("NextBeatButton")
        )
        .Spacing(20)
        .VCenter();
    }
}
