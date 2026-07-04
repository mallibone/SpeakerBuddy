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
                .FontAttributes(FontAttributes.Bold),

            Label("These values are used for every Copy in this run.")
                .FontSize(14)
                .TextColor(Colors.Gray),

            VStack(_names.Select(RenderEntry).ToArray())
                .Spacing(12),

            Button("Continue").OnClicked(Submit).AutomationId("PlaceholderContinueButton")
        )
        .Spacing(16)
        .Padding(16);

    private VisualNode RenderEntry(string name)
        => VStack(
            Label($"{{{{{name}}}}}").FontAttributes(FontAttributes.Bold),
            Entry()
                .Text(State.Values.GetValueOrDefault(name, string.Empty))
                .OnTextChanged(text => SetState(s => s.Values[name] = text))
                .AutomationId($"PlaceholderEntry-{name}")
        )
        .Spacing(4);

    private void Submit() => _onSubmit?.Invoke(State.Values);
}
