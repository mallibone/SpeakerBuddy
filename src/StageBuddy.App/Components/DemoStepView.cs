using StageBuddy.Core.Models;

namespace StageBuddy.App.Components;

partial class DemoStepView : Component
{
    [Prop]
    DemoStep? _step;

    [Prop]
    Action? _onCopy;

    [Prop]
    bool _copyFailed;

    public override VisualNode Render()
    {
        if (_step is null)
            return VStack();

        return VStack(
            Label(_step.Note)
                .FontSize(18),

            _step.Snippet is not null
                ? Label(_step.Snippet)
                    .FontFamily("Courier")
                    .TextColor(Colors.White)
                    .BackgroundColor(Colors.Black)
                    .Padding(12)
                : null,

            Button("Copy")
                .OnClicked(() => _onCopy?.Invoke())
                .HeightRequest(72),

            _copyFailed
                ? Label("Copy failed — try again")
                    .TextColor(Colors.Red)
                : null
        )
        .Spacing(12);
    }
}
