namespace StageBuddy.Core.Validation;

public sealed record ValidationError(string Path, string Message);
