namespace StageBuddy.Core.Models;

public sealed record TalkDocument(SessionInfo Session, IReadOnlyList<Beat> Timeline);
