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
                ? RenderSnippetCard(_step.Snippet, _step.Language)
                : null,

            Button("Copy")
                .OnClicked(() => _onCopy?.Invoke())
                .ImageSource(Icons.Glyph(Icons.Copy, ApplicationTheme.OnAccent, 24))
                .FontSize(20)
                .HeightRequest(72)
                .AutomationId("DemoCopyButton"),

            _copyFailed
                ? HStack(
                    Label(Icons.Warning)
                        .FontFamily(Icons.FontFamily)
                        .FontSize(16)
                        .TextColor(ApplicationTheme.PaceBehind)
                        .VCenter(),
                    Label("Copy failed — try again")
                        .TextColor(ApplicationTheme.PaceBehind)
                        .VCenter()
                        .AutomationId("DemoCopyFailedLabel")
                  )
                  .Spacing(6)
                  .HCenter()
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

    private static VisualNode RenderSnippetCard(string snippet, string? language)
        => Border(
            VStack(
                language is not null
                    ? Label(language.ToUpperInvariant())
                        .FontSize(11)
                        .FontFamily("OpenSansSemibold")
                        .TextColor(ApplicationTheme.Amber300)
                        .CharacterSpacing(1.5)
                    : null,

                Label(snippet)
                    .FontFamily("Courier")
                    .FontSize(15)
                    .TextColor(ApplicationTheme.CodeForeground)
                    .AutomationId("DemoSnippetLabel")
            )
            .Spacing(8)
        )
        .BackgroundColor(ApplicationTheme.CodeBackground)
        .Padding(14);

    private VisualNode? RenderQrToggle()
    {
        if (_isSenderConnected)
        {
            return Button(State.ShowQrManually ? "Hide QR code" : "Show QR code")
                .OnClicked(() => SetState(s => s.ShowQrManually = !s.ShowQrManually))
                .ImageSource(Icons.Glyph(Icons.Qr, ApplicationTheme.Accent, 18))
                .ThemeKey("Secondary")
                .AutomationId("DemoQrToggleButton");
        }

        return null;
    }

    private VisualNode RenderFallbackDisclosure(string fallbackNote)
    {
        return VStack(
            Button(State.ShowFallback ? "Hide fallback note" : "If the demo breaks…")
                .OnClicked(() => SetState(s => s.ShowFallback = !s.ShowFallback))
                .ImageSource(Icons.Glyph(Icons.Warning, ApplicationTheme.Accent, 18))
                .ThemeKey("Secondary")
                .AutomationId("DemoFallbackToggleButton"),

            State.ShowFallback
                ? Border(
                    Label(fallbackNote)
                        .FontAttributes(FontAttributes.Italic)
                        .AutomationId("DemoFallbackNoteLabel")
                  )
                  .BackgroundColor(ApplicationTheme.Card)
                  .Padding(12)
                : null
        )
        .Spacing(8);
    }
}
