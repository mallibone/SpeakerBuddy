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
                    Editor()
                        .Text(State.Json)
                        .OnTextChanged(text => SetState(s => s.Json = text))
                        .HeightRequest(280)
                        .FontFamily("Courier"),

                    Button("Paste from Clipboard")
                        .OnClicked(PasteFromClipboard),

                    Button(State.IsImporting ? "Importing…" : "Import")
                        .OnClicked(Import)
                        .IsEnabled(!State.IsImporting && !string.IsNullOrWhiteSpace(State.Json)),

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
        nodes.AddRange(State.Errors.Select(e => (VisualNode)Label($"{e.Path}: {e.Message}").TextColor(Colors.Red)));
        nodes.AddRange(State.Warnings.Select(w => (VisualNode)Label(w).TextColor(Colors.Orange)));

        return VStack(nodes.ToArray()).Spacing(4);
    }

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
