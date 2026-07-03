using StageBuddy.Core.Models;
using StageBuddy.Core.Storage;

namespace StageBuddy.Core.Tests;

public sealed class FileTalkRepositoryTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("stagebuddy-tests-").FullName;
    private readonly FileTalkRepository _sut;
    private CancellationToken Ct => TestContext.Current.CancellationToken;

    public FileTalkRepositoryTests()
    {
        _sut = new FileTalkRepository(_root);
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private static TalkDocument SampleTalk(string title = "A Talk") => new(
        new SessionInfo(title, 10),
        [new Beat("beat-1", BeatType.Talk, "Intro", 10, null)]);

    [Fact]
    public async Task SaveAndLoad_RoundTrips()
    {
        var talk = SampleTalk();
        await _sut.SaveAsync("talk-1", talk, Ct);

        var loaded = await _sut.LoadAsync("talk-1", Ct);

        Assert.NotNull(loaded);
        Assert.Equal(talk.Session.Title, loaded!.Session.Title);
        Assert.Equal(talk.Timeline.Count, loaded.Timeline.Count);
    }

    [Fact]
    public async Task Load_UnknownTalkId_ReturnsNull()
    {
        var loaded = await _sut.LoadAsync("does-not-exist", Ct);

        Assert.Null(loaded);
    }

    [Fact]
    public async Task List_OnEmptyRoot_ReturnsEmpty()
    {
        var summaries = await _sut.ListAsync(Ct);

        Assert.Empty(summaries);
    }

    [Fact]
    public async Task List_ReturnsSummaryForEachSavedTalk()
    {
        await _sut.SaveAsync("talk-1", SampleTalk("Talk One"), Ct);
        await _sut.SaveAsync("talk-2", SampleTalk("Talk Two"), Ct);

        var summaries = await _sut.ListAsync(Ct);

        Assert.Equal(2, summaries.Count);
        Assert.Contains(summaries, s => s.TalkId == "talk-1" && s.Title == "Talk One");
        Assert.Contains(summaries, s => s.TalkId == "talk-2" && s.Title == "Talk Two");
    }

    [Fact]
    public async Task List_SkipsCorruptedFile_AndContinues()
    {
        await _sut.SaveAsync("good", SampleTalk("Good Talk"), Ct);
        Directory.CreateDirectory(Path.Combine(_root, "talks"));
        await File.WriteAllTextAsync(Path.Combine(_root, "talks", "corrupt.json"), "{ not valid json", Ct);

        var summaries = await _sut.ListAsync(Ct);

        Assert.Single(summaries);
        Assert.Equal("good", summaries[0].TalkId);
    }

    [Fact]
    public async Task Delete_RemovesTalk()
    {
        await _sut.SaveAsync("talk-1", SampleTalk(), Ct);

        await _sut.DeleteAsync("talk-1", Ct);

        Assert.Null(await _sut.LoadAsync("talk-1", Ct));
    }

    [Fact]
    public async Task SaveAsync_OverwritesExistingFile()
    {
        await _sut.SaveAsync("talk-1", SampleTalk("Original"), Ct);
        await _sut.SaveAsync("talk-1", SampleTalk("Updated"), Ct);

        var loaded = await _sut.LoadAsync("talk-1", Ct);

        Assert.Equal("Updated", loaded!.Session.Title);
    }
}
