using StageBuddy.Core.Import;
using StageBuddy.Core.Storage;
using StageBuddy.Core.Validation;

namespace StageBuddy.App.Pages;

class ImportPageProps
{
    public Action? OnImported { get; set; }
}

class ImportPageState
{
    public string Json { get; set; } = string.Empty;
    public IReadOnlyList<ValidationError> Errors { get; set; } = [];
    public IReadOnlyList<string> Warnings { get; set; } = [];
    public bool IsImporting { get; set; }
}

class ImportPage : Component<ImportPageState, ImportPageProps>
{
    private ITalkRepository Repository => Services.GetRequiredService<ITalkRepository>();

    public override VisualNode Render()
        => ContentPage(
            ScrollView(
                VStack(
                    Label("Paste the talk JSON exported from your notes.")
                        .FontSize(14)
                        .TextColor(ApplicationTheme.TextSecondary),

                    Border(
                        Editor()
                            .Text(State.Json)
                            .OnTextChanged(text => SetState(s => s.Json = text))
                            .HeightRequest(280)
                            .FontFamily("Courier")
                            .FontSize(13)
                            .TextColor(ApplicationTheme.CodeForeground)
                            .AutomationId("ImportJsonEditor")
                    )
                    .BackgroundColor(ApplicationTheme.CodeBackground)
                    .Padding(10, 6),

                    Button("Paste from Clipboard")
                        .OnClicked(PasteFromClipboard)
                        .ImageSource(Icons.Glyph(Icons.Paste, ApplicationTheme.Accent, 18))
                        .ThemeKey("Secondary")
                        .AutomationId("ImportPasteButton"),

                    Button(State.IsImporting ? "Importing…" : "Import")
                        .OnClicked(Import)
                        .ImageSource(Icons.Glyph(Icons.Import, ApplicationTheme.OnAccent, 18))
                        .HeightRequest(52)
                        .IsEnabled(!State.IsImporting && !string.IsNullOrWhiteSpace(State.Json))
                        .AutomationId("ImportSubmitButton"),

                    RenderMessages()
                )
                .Spacing(12)
                .Padding(16)
            )
        )
        .Title("Import Talk");

    private VisualNode? RenderMessages()
    {
        if (State.Errors.Count == 0 && State.Warnings.Count == 0)
            return null;

        var nodes = new List<VisualNode>();
        nodes.AddRange(State.Errors.Select(e => RenderMessage($"{e.Path}: {e.Message}", ApplicationTheme.PaceBehind)));
        nodes.AddRange(State.Warnings.Select(w => RenderMessage(w, ApplicationTheme.PaceTolerance)));

        return VStack(nodes.ToArray()).Spacing(6).AutomationId("ImportMessages");
    }

    private static VisualNode RenderMessage(string text, Color color)
        => HStack(
            Label(Icons.Warning)
                .FontFamily(Icons.FontFamily)
                .FontSize(14)
                .TextColor(color),
            Label(text)
                .TextColor(color)
        )
        .Spacing(6);

    private async void PasteFromClipboard()
    {
        var text = await Clipboard.Default.GetTextAsync();
        if (text is not null)
            SetState(s => s.Json = text);
    }

    private async void Import()
    {
        SetState(s => s.IsImporting = true);

        var importer = new TalkImporter(Repository);
        var result = await importer.ImportFromJsonAsync(State.Json);

        if (result.Success)
        {
            Props.OnImported?.Invoke();
            await Navigation!.PopAsync();
        }
        else
        {
            SetState(s =>
            {
                s.IsImporting = false;
                s.Errors = result.Errors;
                s.Warnings = result.Warnings;
            });
        }
    }
}
