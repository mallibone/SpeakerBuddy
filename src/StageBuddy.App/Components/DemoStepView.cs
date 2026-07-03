using StageBuddy.Core.Models;

namespace StageBuddy.App.Components;

class DemoStepViewState
{
    public string? LastSeenStepId { get; set; }
    public bool ShowFallback { get; set; }
}

partial class DemoStepView : Component<DemoStepViewState>
{
    [Prop]
    DemoStep? _step;

    [Prop]
    Action? _onCopy;

    [Prop]
    bool _copyFailed;

    protected override void OnPropsChanged()
    {
        base.OnPropsChanged();

        if (State.LastSeenStepId != _step?.Id)
        {
            SetState(s =>
            {
                s.LastSeenStepId = _step?.Id;
                s.ShowFallback = false;
            });
        }
    }

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
                : null,

            _step.FallbackNote is not null
                ? RenderFallbackDisclosure(_step.FallbackNote)
                : null
        )
        .Spacing(12);
    }

    private VisualNode RenderFallbackDisclosure(string fallbackNote)
    {
        return VStack(
            Button(State.ShowFallback ? "Hide fallback note" : "If the demo breaks…")
                .OnClicked(() => SetState(s => s.ShowFallback = !s.ShowFallback)),

            State.ShowFallback
                ? Label(fallbackNote)
                    .FontAttributes(FontAttributes.Italic)
                : null
        );
    }
}
