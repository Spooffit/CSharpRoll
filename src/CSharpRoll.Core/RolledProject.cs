namespace CSharpRoll.Core;


/// <summary>
/// Snapshot of a project: raw csproj + collected source files.
/// </summary>
public sealed record RolledProject(
    string Name,
    string CsprojPath,
    string CsprojRaw,
    IReadOnlyList<string> Files);