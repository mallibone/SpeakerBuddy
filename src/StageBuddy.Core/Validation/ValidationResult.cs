namespace StageBuddy.Core.Validation;

public sealed record ValidationResult(bool IsValid, IReadOnlyList<ValidationError> Errors, IReadOnlyList<string> Warnings);
