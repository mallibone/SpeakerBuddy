using StageBuddy.Core.Import;
using StageBuddy.Core.Storage;

namespace StageBuddy.Core.Tests;

public sealed class TalkImporterTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("stagebuddy-importer-tests-").FullName;
    private readonly FileTalkRepository _repository;
    private readonly TalkImporter _sut;
    private CancellationToken Ct => TestContext.Current.CancellationToken;

    public TalkImporterTests()
    {
        _repository = new FileTalkRepository(_root);
        _sut = new TalkImporter(_repository);
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private static string ValidJson(string title = "A Talk", string introTitle = "Intro") => $$"""
        {
          "session": { "title": "{{title}}", "plannedDurationMinutes": 5 },
          "timeline": [
            { "id": "beat-1", "type": "talk", "title": "{{introTitle}}", "plannedDurationMinutes": 5 }
          ]
        }
        """;

    [Fact]
    public async Task HappyPath_PersistsAndReturnsSummary()
    {
        var result = await _sut.ImportFromJsonAsync(ValidJson(), Ct);

        Assert.True(result.Success);
        Assert.NotNull(result.Summary);
        Assert.Equal("A Talk", result.Summary!.Title);
        Assert.Equal(1, result.Summary.BeatCount);

        var loaded = await _repository.LoadAsync(result.Summary.TalkId, Ct);
        Assert.NotNull(loaded);
        Assert.Equal("A Talk", loaded!.Session.Title);
    }

    [Fact]
    public async Task InvalidJson_ReturnsErrors_WithoutPersisting()
    {
        var result = await _sut.ImportFromJsonAsync("{ not valid json", Ct);

        Assert.False(result.Success);
        Assert.Null(result.Summary);
        Assert.NotEmpty(result.Errors);

        Assert.Empty(await _repository.ListAsync(Ct));
    }

    [Fact]
    public async Task FailingValidation_ReturnsErrors_WithoutPersisting()
    {
        const string json = """
            {
              "session": { "title": "", "plannedDurationMinutes": 5 },
              "timeline": []
            }
            """;

        var result = await _sut.ImportFromJsonAsync(json, Ct);

        Assert.False(result.Success);
        Assert.Null(result.Summary);
        Assert.NotEmpty(result.Errors);

        Assert.Empty(await _repository.ListAsync(Ct));
    }

    [Fact]
    public async Task ReimportingSameContent_OverwritesRatherThanDuplicates()
    {
        var json = ValidJson();

        var first = await _sut.ImportFromJsonAsync(json, Ct);
        var second = await _sut.ImportFromJsonAsync(json, Ct);

        Assert.True(first.Success);
        Assert.True(second.Success);
        Assert.Equal(first.Summary!.TalkId, second.Summary!.TalkId);

        Assert.Single(await _repository.ListAsync(Ct));
    }

    [Fact]
    public async Task DifferentContent_SameTitle_ProducesDifferentIds()
    {
        var first = await _sut.ImportFromJsonAsync(ValidJson(), Ct);
        var second = await _sut.ImportFromJsonAsync(ValidJson(introTitle: "Intro & Motivation"), Ct);

        Assert.NotEqual(first.Summary!.TalkId, second.Summary!.TalkId);

        Assert.Equal(2, (await _repository.ListAsync(Ct)).Count);
    }
}
