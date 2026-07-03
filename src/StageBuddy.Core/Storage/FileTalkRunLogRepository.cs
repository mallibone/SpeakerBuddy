using System.Text.Json;
using StageBuddy.Core.Serialization;

namespace StageBuddy.Core.Storage;

public sealed class FileTalkRunLogRepository(string rootPath) : ITalkRunLogRepository
{
    private string RunsDirectory(string talkId) => Path.Combine(rootPath, "runs", talkId);

    public async Task SaveAsync(TalkRunLog runLog, CancellationToken ct = default)
    {
        var directory = RunsDirectory(runLog.TalkId);
        Directory.CreateDirectory(directory);

        var fileName = $"{runLog.StartedAt.UtcDateTime:yyyyMMdd-HHmmss}.json";
        await using var stream = File.Create(Path.Combine(directory, fileName));
        await JsonSerializer.SerializeAsync(stream, runLog, TalkJsonContext.Default.TalkRunLog, ct);
    }

    public Task<IReadOnlyList<TalkRunLog>> ListAsync(string talkId, CancellationToken ct = default)
    {
        var directory = RunsDirectory(talkId);
        if (!Directory.Exists(directory))
            return Task.FromResult<IReadOnlyList<TalkRunLog>>([]);

        var runLogs = new List<TalkRunLog>();

        foreach (var file in Directory.EnumerateFiles(directory, "*.json"))
        {
            TalkRunLog? runLog;
            try
            {
                runLog = JsonSerializer.Deserialize(File.ReadAllText(file), TalkJsonContext.Default.TalkRunLog);
            }
            catch (JsonException)
            {
                continue;
            }

            if (runLog is not null)
                runLogs.Add(runLog);
        }

        return Task.FromResult<IReadOnlyList<TalkRunLog>>(runLogs);
    }

    public async Task<TalkRunLog?> GetLatestAsync(string talkId, CancellationToken ct = default)
    {
        var runLogs = await ListAsync(talkId, ct);
        return runLogs.OrderByDescending(r => r.StartedAt).FirstOrDefault();
    }
}
