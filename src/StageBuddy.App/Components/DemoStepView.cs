using StageBuddy.Core.Models;

namespace StageBuddy.App.Components;

class DemoStepViewState
{
    public string? LastSeenStepId { get; set; }
    public bool ShowFallback { get; set; }
    public bool ShowQrManually { get; set; }
}

partial class DemoStepView : Component<DemoStepViewState>
{
    [Prop]
    DemoStep? _step;

    [Prop]
    Action? _onCopy;

    [Prop]
    bool _copyFailed;

    [Prop]
    bool _isSenderConnected = true;

    protected override void OnPropsChanged()
    {
        base.OnPropsChanged();

        if (State.LastSeenStepId != _step?.Id)
        {
            SetState(s =>
            {
                s.LastSeenStepId = _step?.Id;
                s.ShowFallback = false;
                s.ShowQrManually = false;
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
                    .AutomationId("DemoSnippetLabel")
                : null,

            Button("Copy")
                .OnClicked(() => _onCopy?.Invoke())
                .HeightRequest(72)
                .AutomationId("DemoCopyButton"),

            _copyFailed
                ? Label("Copy failed — try again")
                    .TextColor(Colors.Red)
                    .AutomationId("DemoCopyFailedLabel")
                : null,

            _step.FallbackNote is not null
                ? RenderFallbackDisclosure(_step.FallbackNote)
                : null,

            RenderQrToggle(),

            !_isSenderConnected || State.ShowQrManually
                ? new QrFallbackView().SnippetText(_step.Snippet)
                : null
        )
        .Spacing(12);
    }

    private VisualNode? RenderQrToggle()
    {
        if (_isSenderConnected)
        {
            return Button(State.ShowQrManually ? "Hide QR code" : "Show QR code")
                .OnClicked(() => SetState(s => s.ShowQrManually = !s.ShowQrManually))
                .AutomationId("DemoQrToggleButton");
        }

        return null;
    }

    private VisualNode RenderFallbackDisclosure(string fallbackNote)
    {
        return VStack(
            Button(State.ShowFallback ? "Hide fallback note" : "If the demo breaks…")
                .OnClicked(() => SetState(s => s.ShowFallback = !s.ShowFallback))
                .AutomationId("DemoFallbackToggleButton"),

            State.ShowFallback
                ? Label(fallbackNote)
                    .FontAttributes(FontAttributes.Italic)
                    .AutomationId("DemoFallbackNoteLabel")
                : null
        );
    }
}
