using StageBuddy.Core.Models;

namespace StageBuddy.Core.Validation;

public static class TalkValidator
{
    public static ValidationResult Validate(TalkDocument document)
    {
        var errors = new List<ValidationError>();
        var warnings = new List<string>();
        var seenIds = new HashSet<string>();

        // The records declare Session/Timeline non-nullable, but deserializing JSON that merely
        // *parses* (e.g. `{ "mangled": true }`) still produces nulls here — guard, don't throw.
        if (document.Session is null || document.Timeline is null)
        {
            if (document.Session is null)
                errors.Add(new ValidationError("session", "Missing required 'session' object."));
            if (document.Timeline is null)
                errors.Add(new ValidationError("timeline", "Missing required 'timeline' array."));
            return new ValidationResult(false, errors, warnings);
        }

        if (string.IsNullOrWhiteSpace(document.Session.Title))
            errors.Add(new ValidationError("session.title", "Session title must not be empty."));

        if (document.Session.PlannedDurationMinutes <= 0)
            errors.Add(new ValidationError("session.plannedDurationMinutes", "Session planned duration must be greater than zero."));

        if (document.Timeline.Count == 0)
            errors.Add(new ValidationError("timeline", "Timeline must contain at least one beat."));

        for (var beatIndex = 0; beatIndex < document.Timeline.Count; beatIndex++)
        {
            if (document.Timeline[beatIndex] is null)
                errors.Add(new ValidationError($"timeline[{beatIndex}]", "Beat must be an object."));
            else
                ValidateBeat(document.Timeline[beatIndex], $"timeline[{beatIndex}]", seenIds, errors);
        }

        var plannedTotal = document.Timeline.Where(beat => beat is not null).Sum(beat => beat.PlannedDurationMinutes);
        if (plannedTotal != document.Session.PlannedDurationMinutes)
        {
            warnings.Add(
                $"Sum of beat planned durations ({plannedTotal}m) does not match session planned duration ({document.Session.PlannedDurationMinutes}m).");
        }

        return new ValidationResult(errors.Count == 0, errors, warnings);
    }

    private static void ValidateBeat(Beat beat, string beatPath, HashSet<string> seenIds, List<ValidationError> errors)
    {
        ValidateId(beat.Id, beatPath, seenIds, errors);

        switch (beat.Type)
        {
            case BeatType.Talk:
                if (beat.Steps is { Count: > 0 })
                    errors.Add(new ValidationError($"{beatPath}.steps", "Talk beats must not have steps."));
                break;

            case BeatType.Demo:
                if (beat.Steps is not { Count: > 0 })
                {
                    errors.Add(new ValidationError($"{beatPath}.steps", "Demo beats must have at least one step."));
                    break;
                }

                for (var stepIndex = 0; stepIndex < beat.Steps.Count; stepIndex++)
                {
                    if (beat.Steps[stepIndex] is null)
                        errors.Add(new ValidationError($"{beatPath}.steps[{stepIndex}]", "Step must be an object."));
                    else
                        ValidateStep(beat.Steps[stepIndex], $"{beatPath}.steps[{stepIndex}]", seenIds, errors);
                }
                break;
        }
    }

    private static void ValidateStep(DemoStep step, string stepPath, HashSet<string> seenIds, List<ValidationError> errors)
    {
        ValidateId(step.Id, stepPath, seenIds, errors);

        if (string.IsNullOrWhiteSpace(step.Note))
            errors.Add(new ValidationError($"{stepPath}.note", "Step note must not be empty."));

        if (step.Snippet is not null && string.IsNullOrWhiteSpace(step.Snippet))
            errors.Add(new ValidationError($"{stepPath}.snippet", "Step snippet, if present, must not be empty."));
    }

    private static void ValidateId(string id, string path, HashSet<string> seenIds, List<ValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            errors.Add(new ValidationError($"{path}.id", "Id must not be empty."));
            return;
        }

        if (!seenIds.Add(id))
            errors.Add(new ValidationError($"{path}.id", $"Duplicate id '{id}'; ids must be globally unique."));
    }
}
