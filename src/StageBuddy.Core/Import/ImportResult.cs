using StageBuddy.Core.Models;
using StageBuddy.Core.Validation;

namespace StageBuddy.Core.Import;

public sealed record ImportResult(bool Success, TalkSummary? Summary, IReadOnlyList<ValidationError> Errors, IReadOnlyList<string> Warnings)
{
    public static ImportResult Succeeded(TalkSummary summary, IReadOnlyList<string> warnings) =>
        new(true, summary, [], warnings);

    public static ImportResult Failed(IReadOnlyList<ValidationError> errors) =>
        new(false, null, errors, []);
}
