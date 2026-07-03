using StageBuddy.Core.Live;
using StageBuddy.Core.Storage;

namespace StageBuddy.Core.Tests;

public sealed class FileTalkRunLogRepositoryTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("stagebuddy-runlog-tests-").FullName;
    private readonly FileTalkRunLogRepository _sut;
    private CancellationToken Ct => TestContext.Current.CancellationToken;

    public FileTalkRunLogRepositoryTests()
    {
        _sut = new FileTalkRunLogRepository(_root);
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private static TalkRunLog SampleRunLog(string talkId = "talk-1", DateTimeOffset? startedAt = null) => new(
        talkId,
        startedAt ?? new DateTimeOffset(2026, 7, 3, 9, 0, 0, TimeSpan.Zero),
        [new BeatTiming("beat-1", TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(6))],
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(6));

    [Fact]
    public async Task Save_ThenList_ReturnsTheRunLog()
    {
        var runLog = SampleRunLog();
        await _sut.SaveAsync(runLog, Ct);

        var runLogs = await _sut.ListAsync("talk-1", Ct);

        Assert.Single(runLogs);
        Assert.Equal(runLog.StartedAt, runLogs[0].StartedAt);
        Assert.Equal(runLog.TotalPlanned, runLogs[0].TotalPlanned);
        Assert.Equal(runLog.TotalActual, runLogs[0].TotalActual);
        Assert.Equal(runLog.BeatTimings, runLogs[0].BeatTimings);
    }

    [Fact]
    public async Task List_UnknownTalkId_ReturnsEmpty()
    {
        var runLogs = await _sut.ListAsync("does-not-exist", Ct);

        Assert.Empty(runLogs);
    }

    [Fact]
    public async Task GetLatest_ReturnsNull_WhenNoRunsExist()
    {
        var latest = await _sut.GetLatestAsync("talk-1", Ct);

        Assert.Null(latest);
    }

    [Fact]
    public async Task GetLatest_ReturnsTheMostRecentlyStartedRun()
    {
        await _sut.SaveAsync(SampleRunLog(startedAt: new DateTimeOffset(2026, 7, 1, 9, 0, 0, TimeSpan.Zero)), Ct);
        await _sut.SaveAsync(SampleRunLog(startedAt: new DateTimeOffset(2026, 7, 3, 9, 0, 0, TimeSpan.Zero)), Ct);
        await _sut.SaveAsync(SampleRunLog(startedAt: new DateTimeOffset(2026, 7, 2, 9, 0, 0, TimeSpan.Zero)), Ct);

        var latest = await _sut.GetLatestAsync("talk-1", Ct);

        Assert.NotNull(latest);
        Assert.Equal(new DateTimeOffset(2026, 7, 3, 9, 0, 0, TimeSpan.Zero), latest!.StartedAt);
    }

    [Fact]
    public async Task RunLogs_ForDifferentTalks_DoNotMix()
    {
        await _sut.SaveAsync(SampleRunLog("talk-1"), Ct);
        await _sut.SaveAsync(SampleRunLog("talk-2"), Ct);

        var talk1Runs = await _sut.ListAsync("talk-1", Ct);
        var talk2Runs = await _sut.ListAsync("talk-2", Ct);

        Assert.Single(talk1Runs);
        Assert.Single(talk2Runs);
    }

    [Fact]
    public async Task List_SkipsCorruptedFile_AndContinues()
    {
        await _sut.SaveAsync(SampleRunLog(), Ct);
        var directory = Path.Combine(_root, "runs", "talk-1");
        await File.WriteAllTextAsync(Path.Combine(directory, "corrupt.json"), "{ not valid json", Ct);

        var runLogs = await _sut.ListAsync("talk-1", Ct);

        Assert.Single(runLogs);
    }
}
