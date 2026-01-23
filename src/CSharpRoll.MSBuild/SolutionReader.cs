using Microsoft.Build.Construction;

namespace CSharpRoll.MSBuild;

/// <summary>
/// Reads C# projects from a .sln file.
/// </summary>
public static class SolutionReader
{
    /// <summary>
    /// Parses the solution and returns all .csproj projects.
    /// </summary>
    public static List<SolutionProjectInfo> ReadCSharpProjects(string slnPath)
    {
        var slnDir = Path.GetDirectoryName(slnPath)!;

        var sln = SolutionFile.Parse(slnPath);
        var list = new List<SolutionProjectInfo>();

        foreach (var p in sln.ProjectsInOrder)
        {
            if (!p.RelativePath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                continue;

            var full = Path.GetFullPath(Path.Combine(slnDir, p.RelativePath));
            if (!File.Exists(full))
                continue;

            list.Add(new SolutionProjectInfo(p.ProjectName, p.RelativePath, full));
        }

        return list
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}