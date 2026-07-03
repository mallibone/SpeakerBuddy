using System.Text.Json;
using StageBuddy.Core.Serialization;
using StageBuddy.Core.Sync;

namespace StageBuddy.Core.Tests;

public class SyncEnvelopeTests
{
    [Fact]
    public void RoundTrips_WithPayload()
    {
        var envelope = new SyncEnvelope(1, "snippet", new SnippetPayload("dotnet build", "bash", "My Talk", "step-1"));

        var json = JsonSerializer.Serialize(envelope, TalkJsonContext.Default.SyncEnvelope);
        var roundTripped = JsonSerializer.Deserialize(json, TalkJsonContext.Default.SyncEnvelope);

        Assert.NotNull(roundTripped);
        Assert.Equal(envelope.V, roundTripped!.V);
        Assert.Equal(envelope.Type, roundTripped.Type);
        Assert.NotNull(roundTripped.Payload);
        Assert.Equal(envelope.Payload!.Text, roundTripped.Payload!.Text);
        Assert.Equal(envelope.Payload.Language, roundTripped.Payload.Language);
        Assert.Equal(envelope.Payload.TalkTitle, roundTripped.Payload.TalkTitle);
        Assert.Equal(envelope.Payload.StepId, roundTripped.Payload.StepId);
    }

    [Fact]
    public void ReservedType_Ping_RoundTrips()
    {
        const string json = """{"v":1,"type":"ping"}""";

        var envelope = JsonSerializer.Deserialize(json, TalkJsonContext.Default.SyncEnvelope);

        Assert.NotNull(envelope);
        Assert.Equal("ping", envelope!.Type);
        Assert.Null(envelope.Payload);
    }

    [Fact]
    public void UnknownFutureType_DoesNotThrow()
    {
        const string json = """{"v":1,"type":"something-from-the-future"}""";

        var exception = Record.Exception(() => JsonSerializer.Deserialize(json, TalkJsonContext.Default.SyncEnvelope));

        Assert.Null(exception);
        var envelope = JsonSerializer.Deserialize(json, TalkJsonContext.Default.SyncEnvelope);
        Assert.Equal("something-from-the-future", envelope!.Type);
    }

    [Fact]
    public void VersionMismatch_IsDetectableByCaller()
    {
        const string json = """{"v":2,"type":"snippet","payload":{"text":"x","talkTitle":"t","stepId":"s"}}""";

        var envelope = JsonSerializer.Deserialize(json, TalkJsonContext.Default.SyncEnvelope);

        Assert.NotNull(envelope);
        Assert.Equal(2, envelope!.V);
        Assert.NotEqual(1, envelope.V);
    }

    [Fact]
    public void PayloadOptionalLanguage_RoundTripsAsNull()
    {
        var payload = new SnippetPayload("code", null, "Talk", "step-2");

        var json = JsonSerializer.Serialize(payload, TalkJsonContext.Default.SnippetPayload);
        var roundTripped = JsonSerializer.Deserialize(json, TalkJsonContext.Default.SnippetPayload);

        Assert.NotNull(roundTripped);
        Assert.Null(roundTripped!.Language);
        Assert.Equal("code", roundTripped.Text);
    }
}
