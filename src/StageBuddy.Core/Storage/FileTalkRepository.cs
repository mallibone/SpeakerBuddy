using System.Text.Json;
using StageBuddy.Core.Models;
using StageBuddy.Core.Serialization;

namespace StageBuddy.Core.Storage;

public sealed class FileTalkRepository(string rootPath) : ITalkRepository
{
    private string TalksDirectory => Path.Combine(rootPath, "talks");

    public async Task SaveAsync(string talkId, TalkDocument talk, CancellationToken ct = default)
    {
        Directory.CreateDirectory(TalksDirectory);

        await using var stream = File.Create(TalkFilePath(talkId));
        await JsonSerializer.SerializeAsync(stream, talk, TalkJsonContext.Default.TalkDocument, ct);
    }

    public async Task<TalkDocument?> LoadAsync(string talkId, CancellationToken ct = default)
    {
        var path = TalkFilePath(talkId);
        if (!File.Exists(path))
            return null;

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync(stream, TalkJsonContext.Default.TalkDocument, ct);
    }

    public Task<IReadOnlyList<TalkSummary>> ListAsync(CancellationToken ct = default)
    {
        if (!Directory.Exists(TalksDirectory))
            return Task.FromResult<IReadOnlyList<TalkSummary>>([]);

        var summaries = new List<TalkSummary>();

        foreach (var file in Directory.EnumerateFiles(TalksDirectory, "*.json"))
        {
            TalkDocument? talk;
            try
            {
                talk = JsonSerializer.Deserialize(File.ReadAllText(file), TalkJsonContext.Default.TalkDocument);
            }
            catch (JsonException)
            {
                continue;
            }

            if (talk is null)
                continue;

            var talkId = Path.GetFileNameWithoutExtension(file);
            summaries.Add(new TalkSummary(talkId, talk.Session.Title, talk.Session.PlannedDurationMinutes, talk.Timeline.Count));
        }

        return Task.FromResult<IReadOnlyList<TalkSummary>>(summaries);
    }

    public Task DeleteAsync(string talkId, CancellationToken ct = default)
    {
        File.Delete(TalkFilePath(talkId));
        return Task.CompletedTask;
    }

    private string TalkFilePath(string talkId) => Path.Combine(TalksDirectory, $"{talkId}.json");
}
