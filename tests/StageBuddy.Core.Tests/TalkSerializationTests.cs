using System.Text.Json;
using System.Text.Json.Nodes;
using StageBuddy.Core.Models;
using StageBuddy.Core.Serialization;

namespace StageBuddy.Core.Tests;

public class TalkSerializationTests
{
    private static string ReadSampleJson() =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", "sample-talk.json"));

    [Fact]
    public void Deserializes_SpecSampleJson_IntoExpectedShape()
    {
        var talk = JsonSerializer.Deserialize(ReadSampleJson(), TalkJsonContext.Default.TalkDocument);

        Assert.NotNull(talk);
        Assert.Equal("Building MCP Servers in .NET", talk!.Session.Title);
        Assert.Equal(45, talk.Session.PlannedDurationMinutes);
        Assert.Equal(2, talk.Timeline.Count);

        var talkBeat = talk.Timeline[0];
        Assert.Equal("beat-1", talkBeat.Id);
        Assert.Equal(BeatType.Talk, talkBeat.Type);
        Assert.Equal("Intro & motivation", talkBeat.Title);
        Assert.Equal(5, talkBeat.PlannedDurationMinutes);
        Assert.Null(talkBeat.Steps);

        var demoBeat = talk.Timeline[1];
        Assert.Equal("beat-2", demoBeat.Id);
        Assert.Equal(BeatType.Demo, demoBeat.Type);
        Assert.Equal(8, demoBeat.PlannedDurationMinutes);
        Assert.NotNull(demoBeat.Steps);
        Assert.Equal(2, demoBeat.Steps!.Count);

        var step1 = demoBeat.Steps[0];
        Assert.Equal("step-1", step1.Id);
        Assert.Equal("dotnet add package ModelContextProtocol", step1.Snippet);
        Assert.Equal("bash", step1.Language);
        Assert.Equal("If live setup breaks, show the pre-built project instead", step1.FallbackNote);

        var step2 = demoBeat.Steps[1];
        Assert.Equal("step-2", step2.Id);
        Assert.Equal("builder.Services.AddMcpServer()\n    .WithStdioServerTransport();", step2.Snippet);
        Assert.Equal("csharp", step2.Language);
        Assert.Null(step2.FallbackNote);
    }

    [Fact]
    public void RoundTrips_SpecSampleJson_Structurally()
    {
        var json = ReadSampleJson();
        var talk = JsonSerializer.Deserialize(json, TalkJsonContext.Default.TalkDocument)!;

        var reserialized = JsonSerializer.Serialize(talk, TalkJsonContext.Default.TalkDocument);

        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(json), JsonNode.Parse(reserialized)));
    }

    [Theory]
    [InlineData(BeatType.Talk, "\"talk\"")]
    [InlineData(BeatType.Demo, "\"demo\"")]
    public void BeatType_SerializesToLowercaseName(BeatType type, string expectedJson)
    {
        var json = JsonSerializer.Serialize(type, TalkJsonContext.Default.BeatType);

        Assert.Equal(expectedJson, json);
    }

    [Theory]
    [InlineData("\"talk\"", BeatType.Talk)]
    [InlineData("\"demo\"", BeatType.Demo)]
    public void BeatType_DeserializesFromLowercaseName(string json, BeatType expected)
    {
        var type = JsonSerializer.Deserialize(json, TalkJsonContext.Default.BeatType);

        Assert.Equal(expected, type);
    }

    [Fact]
    public void PropertyNames_AreCamelCase()
    {
        var talk = new TalkDocument(
            new SessionInfo("My Session", 10),
            [new Beat("beat-1", BeatType.Talk, "Intro", 5, null)]);

        var json = JsonSerializer.Serialize(talk, TalkJsonContext.Default.TalkDocument);

        Assert.Contains("\"plannedDurationMinutes\"", json);
        Assert.DoesNotContain("\"PlannedDurationMinutes\"", json);
        Assert.DoesNotContain("\"Title\":", json);
        Assert.Contains("\"title\":", json);
    }

    [Fact]
    public void DemoStep_OptionalFields_SerializeAsNullWhenAbsent()
    {
        var step = new DemoStep("step-1", "Note only", null, null, null);

        var json = JsonSerializer.Serialize(step, TalkJsonContext.Default.DemoStep);
        var roundTripped = JsonSerializer.Deserialize(json, TalkJsonContext.Default.DemoStep);

        Assert.NotNull(roundTripped);
        Assert.Null(roundTripped!.Snippet);
        Assert.Null(roundTripped.Language);
        Assert.Null(roundTripped.FallbackNote);
    }
}
