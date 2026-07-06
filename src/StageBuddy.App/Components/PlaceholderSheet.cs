namespace StageBuddy.App.Components;

class PlaceholderSheetState
{
    public Dictionary<string, string> Values { get; set; } = [];
}

partial class PlaceholderSheet : Component<PlaceholderSheetState>
{
    [Prop]
    IReadOnlyList<string> _names = [];

    [Prop]
    IReadOnlyDictionary<string, string> _defaults = new Dictionary<string, string>();

    [Prop]
    Action<IReadOnlyDictionary<string, string>>? _onSubmit;

    protected override void OnMounted()
    {
        base.OnMounted();
        SetState(s => s.Values = new Dictionary<string, string>(_defaults));
    }

    public override VisualNode Render()
        => VStack(
            Label("Fill in placeholders before copying")
                .FontSize(18)
                .FontFamily("OpenSansSemibold"),

            Label("These values are used for every Copy in this run.")
                .FontSize(14)
                .TextColor(ApplicationTheme.TextSecondary),

            VStack(_names.Select(RenderEntry).ToArray())
                .Spacing(12),

            Button("Continue")
                .OnClicked(Submit)
                .ImageSource(Icons.Glyph(Icons.Check, ApplicationTheme.OnAccent, 18))
                .HeightRequest(52)
                .AutomationId("PlaceholderContinueButton")
        )
        .Spacing(16)
        .Padding(16);

    private VisualNode RenderEntry(string name)
        => VStack(
            Label($"{{{{{name}}}}}")
                .FontFamily("Courier")
                .FontSize(15)
                .TextColor(ApplicationTheme.Accent),
            Border(
                Entry()
                    .Text(State.Values.GetValueOrDefault(name, string.Empty))
                    .OnTextChanged(text => SetState(s => s.Values[name] = text))
                    .AutomationId($"PlaceholderEntry-{name}")
            )
            .BackgroundColor(ApplicationTheme.Card)
            .Padding(8, 0)
        )
        .Spacing(6);

    private void Submit() => _onSubmit?.Invoke(State.Values);
}
