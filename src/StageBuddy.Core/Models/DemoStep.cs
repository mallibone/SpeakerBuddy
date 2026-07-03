namespace StageBuddy.Core.Models;

public sealed record DemoStep(
    string Id,
    string Note,
    string? Snippet,
    string? Language,
    string? FallbackNote);
