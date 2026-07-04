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
                .FontSize(28)
                .FontAttributes(FontAttributes.Bold)
                .HCenter(),

            Label($"{_beat.PlannedDurationMinutes} min planned")
                .TextColor(Colors.Gray)
                .HCenter(),

            Button("Next beat")
                .OnClicked(() => _onNext?.Invoke())
                .HeightRequest(64)
                .AutomationId("NextBeatButton")
        )
        .Spacing(16)
        .VCenter();
    }
}
