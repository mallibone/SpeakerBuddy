using Microsoft.Extensions.Time.Testing;
using StageBuddy.Core.Placeholders;

namespace StageBuddy.Core.Tests;

public class PlaceholderEngineTests
{
    [Fact]
    public void DetectPlaceholders_ReturnsDistinctNamesInFirstOccurrenceOrder()
    {
        var engine = new PlaceholderEngine(new FakeTimeProvider());

        var names = engine.DetectPlaceholders("curl {{host}}/api/{{env}}?token={{host}}");

        Assert.Equal(["host", "env"], names);
    }

    [Fact]
    public void DetectPlaceholders_NoPlaceholders_ReturnsEmpty()
    {
        var engine = new PlaceholderEngine(new FakeTimeProvider());

        var names = engine.DetectPlaceholders("dotnet build");

        Assert.Empty(names);
    }

    [Fact]
    public void Substitute_ReplacesEveryOccurrenceOfAKnownName()
    {
        var engine = new PlaceholderEngine(new FakeTimeProvider());

        var result = engine.Substitute(
            "curl {{host}}/api/{{env}}?token={{host}}",
            new Dictionary<string, string> { ["host"] = "example.com", ["env"] = "prod" });

        Assert.Equal("curl example.com/api/prod?token=example.com", result);
    }

    [Fact]
    public void Substitute_UnknownPlaceholder_IsLeftIntact()
    {
        var engine = new PlaceholderEngine(new FakeTimeProvider());

        var result = engine.Substitute("token={{secret}}", new Dictionary<string, string>());

        Assert.Equal("token={{secret}}", result);
    }

    [Fact]
    public void Substitute_BuiltInDate_ResolvesFromTimeProviderWhenNotOverridden()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 7, 3, 0, 0, 0, TimeSpan.Zero));
        var engine = new PlaceholderEngine(clock);

        var result = engine.Substitute("day={{date}}", new Dictionary<string, string>());

        Assert.Equal("day=2026-07-03", result);
    }

    [Fact]
    public void Substitute_BuiltInDate_UserOverrideTakesPrecedence()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 7, 3, 0, 0, 0, TimeSpan.Zero));
        var engine = new PlaceholderEngine(clock);

        var result = engine.Substitute("day={{date}}", new Dictionary<string, string> { ["date"] = "2099-01-01" });

        Assert.Equal("day=2099-01-01", result);
    }

    [Theory]
    [InlineData("dangling {{ open brace")]
    [InlineData("stray }} close brace")]
    [InlineData("{{unclosed")]
    public void Substitute_MalformedBraces_NeverThrowsAndLeavesInputUnchanged(string snippet)
    {
        var engine = new PlaceholderEngine(new FakeTimeProvider());

        var result = engine.Substitute(snippet, new Dictionary<string, string>());

        Assert.Equal(snippet, result);
    }

    [Fact]
    public void Substitute_ValueContainingSpecialCharacters_IsInsertedVerbatim()
    {
        var engine = new PlaceholderEngine(new FakeTimeProvider());

        var result = engine.Substitute(
            "key={{value}}",
            new Dictionary<string, string> { ["value"] = "a$1\\b{{nested}}$&" });

        Assert.Equal("key=a$1\\b{{nested}}$&", result);
    }
}
