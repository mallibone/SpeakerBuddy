namespace StageBuddy.Core.Sync;

public sealed record SyncEnvelope(int V, string Type, SnippetPayload? Payload);
