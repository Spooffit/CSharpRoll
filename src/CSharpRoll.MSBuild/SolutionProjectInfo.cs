namespace CSharpRoll.MSBuild;

/// <summary>
/// Represents a C# project entry discovered in a solution.
/// </summary>
public sealed record SolutionProjectInfo(string Name, string RelativePath, string FullPath);