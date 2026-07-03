using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using StageBuddy.Core.Models;
using StageBuddy.Core.Serialization;
using StageBuddy.Core.Storage;
using StageBuddy.Core.Validation;

namespace StageBuddy.Core.Import;

public sealed class TalkImporter(ITalkRepository repository)
{
    public async Task<ImportResult> ImportFromJsonAsync(string json, CancellationToken ct = default)
    {
        TalkDocument document;
        try
        {
            document = JsonSerializer.Deserialize(json, TalkJsonContext.Default.TalkDocument)
                       ?? throw new JsonException("JSON deserialized to an empty document.");
        }
        catch (JsonException ex)
        {
            return ImportResult.Failed([new ValidationError("$", $"Invalid JSON: {ex.Message}")]);
        }

        var validation = TalkValidator.Validate(document);
        if (!validation.IsValid)
            return ImportResult.Failed(validation.Errors);

        var talkId = DeriveTalkId(document.Session.Title, json);
        await repository.SaveAsync(talkId, document, ct);

        var summary = new TalkSummary(talkId, document.Session.Title, document.Session.PlannedDurationMinutes, document.Timeline.Count);
        return ImportResult.Succeeded(summary, validation.Warnings);
    }

    private static string DeriveTalkId(string title, string content)
    {
        var slug = Slugify(title);
        var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(content)))[..8];
        return string.IsNullOrEmpty(slug) ? hash : $"{slug}-{hash}";
    }

    private static string Slugify(string title)
    {
        var builder = new StringBuilder(title.Length);
        var lastWasHyphen = false;

        foreach (var ch in title.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(ch);
                lastWasHyphen = false;
            }
            else if (!lastWasHyphen && builder.Length > 0)
            {
                builder.Append('-');
                lastWasHyphen = true;
            }
        }

        return builder.ToString().TrimEnd('-');
    }
}
