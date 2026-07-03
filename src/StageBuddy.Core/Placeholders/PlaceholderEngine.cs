using System.Text.RegularExpressions;

namespace StageBuddy.Core.Placeholders;

public sealed partial class PlaceholderEngine(TimeProvider clock)
{
    private const string BuiltInDateName = "date";

    public IReadOnlyList<string> DetectPlaceholders(string snippet)
    {
        var seen = new HashSet<string>();
        var names = new List<string>();

        foreach (Match match in PlaceholderPattern().Matches(snippet))
        {
            var name = match.Groups[1].Value;
            if (seen.Add(name))
                names.Add(name);
        }

        return names;
    }

    public string Substitute(string snippet, IReadOnlyDictionary<string, string> values)
    {
        return PlaceholderPattern().Replace(snippet, match =>
        {
            var name = match.Groups[1].Value;

            if (values.TryGetValue(name, out var value))
                return value;

            return name == BuiltInDateName
                ? clock.GetUtcNow().ToString("yyyy-MM-dd")
                : match.Value;
        });
    }

    [GeneratedRegex(@"\{\{(\w+)\}\}")]
    private static partial Regex PlaceholderPattern();
}
