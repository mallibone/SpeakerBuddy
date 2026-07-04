using StageBuddy.Core.Models;
using StageBuddy.Core.Validation;

namespace StageBuddy.Core.Tests;

public class TalkValidatorTests
{
    private static TalkDocument ValidDocument() => new(
        new SessionInfo("A Talk", 13),
        [
            new Beat("beat-1", BeatType.Talk, "Intro", 5, null),
            new Beat("beat-2", BeatType.Demo, "Demo", 8,
                [new DemoStep("step-1", "Show something", "dotnet run", "bash", null)])
        ]);

    [Fact]
    public void ValidDocument_IsValid_NoErrors_NoWarnings()
    {
        var result = TalkValidator.Validate(ValidDocument());

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void EmptySessionTitle_ProducesError()
    {
        var document = ValidDocument() with { Session = new SessionInfo("  ", 13) };

        var result = TalkValidator.Validate(document);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Path == "session.title");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void NonPositivePlannedDuration_ProducesError(int minutes)
    {
        var document = ValidDocument() with { Session = new SessionInfo("A Talk", minutes) };

        var result = TalkValidator.Validate(document);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Path == "session.plannedDurationMinutes");
    }

    [Fact]
    public void EmptyTimeline_ProducesError()
    {
        var document = ValidDocument() with { Timeline = [] };

        var result = TalkValidator.Validate(document);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Path == "timeline");
    }

    [Fact]
    public void DuplicateBeatIds_ProducesError()
    {
        var document = ValidDocument() with
        {
            Timeline =
            [
                new Beat("dup", BeatType.Talk, "Intro", 5, null),
                new Beat("dup", BeatType.Talk, "Also intro", 5, null)
            ]
        };

        var result = TalkValidator.Validate(document);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Path == "timeline[1].id");
    }

    [Fact]
    public void DuplicateIdAcrossBeatAndStep_ProducesError()
    {
        var document = ValidDocument() with
        {
            Timeline =
            [
                new Beat("shared", BeatType.Talk, "Intro", 5, null),
                new Beat("beat-2", BeatType.Demo, "Demo", 8,
                    [new DemoStep("shared", "Note", null, null, null)])
            ]
        };

        var result = TalkValidator.Validate(document);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Path == "timeline[1].steps[0].id");
    }

    [Fact]
    public void DemoBeatWithNoSteps_ProducesError()
    {
        var document = ValidDocument() with
        {
            Timeline = [new Beat("beat-1", BeatType.Demo, "Demo", 5, null)]
        };

        var result = TalkValidator.Validate(document);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Path == "timeline[0].steps");
    }

    [Fact]
    public void DemoBeatWithEmptyStepsArray_ProducesError()
    {
        var document = ValidDocument() with
        {
            Timeline = [new Beat("beat-1", BeatType.Demo, "Demo", 5, [])]
        };

        var result = TalkValidator.Validate(document);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Path == "timeline[0].steps");
    }

    [Fact]
    public void TalkBeatWithSteps_ProducesError()
    {
        var document = ValidDocument() with
        {
            Timeline =
            [
                new Beat("beat-1", BeatType.Talk, "Intro", 5,
                    [new DemoStep("step-1", "Note", null, null, null)])
            ]
        };

        var result = TalkValidator.Validate(document);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Path == "timeline[0].steps");
    }

    [Fact]
    public void StepWithEmptyNote_ProducesError()
    {
        var document = ValidDocument() with
        {
            Timeline =
            [
                new Beat("beat-1", BeatType.Demo, "Demo", 5,
                    [new DemoStep("step-1", "  ", null, null, null)])
            ]
        };

        var result = TalkValidator.Validate(document);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Path == "timeline[0].steps[0].note");
    }

    [Fact]
    public void StepWithWhitespaceSnippet_ProducesError()
    {
        var document = ValidDocument() with
        {
            Timeline =
            [
                new Beat("beat-1", BeatType.Demo, "Demo", 5,
                    [new DemoStep("step-1", "Note", "   ", null, null)])
            ]
        };

        var result = TalkValidator.Validate(document);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Path == "timeline[0].steps[0].snippet");
    }

    [Fact]
    public void StepWithNullSnippet_IsValid()
    {
        var document = ValidDocument() with
        {
            Timeline =
            [
                new Beat("beat-1", BeatType.Demo, "Demo", 5,
                    [new DemoStep("step-1", "Note", null, null, null)])
            ]
        };

        var result = TalkValidator.Validate(document);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void DurationMismatch_IsWarningNotError()
    {
        var document = ValidDocument() with { Session = new SessionInfo("A Talk", 99) };

        var result = TalkValidator.Validate(document);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Single(result.Warnings);
    }

    [Fact]
    public void MultipleErrors_AreAllCollected_NotJustFirst()
    {
        var document = new TalkDocument(
            new SessionInfo("", 0),
            [new Beat("", BeatType.Demo, "Demo", 5, null)]);

        var result = TalkValidator.Validate(document);

        Assert.False(result.IsValid);
        Assert.True(result.Errors.Count >= 4);
    }

    // Deserializing JSON that parses but has the wrong shape (e.g. { "mangled": true })
    // produces nulls despite the records' non-nullable declarations. Found live by the
    // simulator e2e suite: the import silently died on an NRE instead of showing errors.

    [Fact]
    public void NullSession_ProducesErrorInsteadOfThrowing()
    {
        var document = new TalkDocument(null!, [new Beat("beat-1", BeatType.Talk, "Intro", 5, null)]);

        var result = TalkValidator.Validate(document);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Path == "session");
    }

    [Fact]
    public void NullTimeline_ProducesErrorInsteadOfThrowing()
    {
        var document = new TalkDocument(new SessionInfo("A Talk", 13), null!);

        var result = TalkValidator.Validate(document);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Path == "timeline");
    }

    [Fact]
    public void NullSessionAndTimeline_BothReported()
    {
        var document = new TalkDocument(null!, null!);

        var result = TalkValidator.Validate(document);

        Assert.False(result.IsValid);
        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public void NullBeatInTimeline_ProducesErrorInsteadOfThrowing()
    {
        var document = ValidDocument() with { Timeline = [null!, new Beat("beat-1", BeatType.Talk, "Intro", 13, null)] };

        var result = TalkValidator.Validate(document);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Path == "timeline[0]");
    }

    [Fact]
    public void NullStepInDemoBeat_ProducesErrorInsteadOfThrowing()
    {
        var document = ValidDocument() with
        {
            Timeline = [new Beat("beat-1", BeatType.Demo, "Demo", 13, [null!])]
        };

        var result = TalkValidator.Validate(document);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Path == "timeline[0].steps[0]");
    }
}
