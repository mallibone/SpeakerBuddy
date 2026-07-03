namespace StageBuddy.Core.Sync;

public sealed record SnippetPayload(string Text, string? Language, string TalkTitle, string StepId);
