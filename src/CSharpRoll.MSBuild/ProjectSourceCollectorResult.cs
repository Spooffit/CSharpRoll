namespace CSharpRoll.MSBuild;

/// <summary>
/// Represents the result of collecting source files from a project.
/// </summary>
public sealed class ProjectSourceCollectorResult
{
    /// <summary>
    /// Gets collected files.
    /// </summary>
    public List<string> Files { get; } = new();

    /// <summary>
    /// Gets warnings.
    /// </summary>
    public List<string> Warnings { get; } = new();
}