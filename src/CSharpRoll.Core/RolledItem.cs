namespace CSharpRoll.Core;

public sealed record RolledItem(
    string ItemType,
    string EvaluatedInclude,
    IReadOnlyDictionary<string, string> Metadata);