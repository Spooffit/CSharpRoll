namespace CSharpRoll.Core;


/// <summary>
/// Provides path filtering rules for excluding build artifacts and generated sources.
/// </summary>
public static class PathFilter
{
    private static readonly string[] ExcludedDirs =
    {
        $"{Path.DirectorySeparatorChar}.git{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}.vs{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}node_modules{Path.DirectorySeparatorChar}",
    };

    /// <summary>
    /// Determines whether the file should be excluded from the roll.
    /// </summary>
    /// <param name="fullPath">Absolute file path.</param>
    /// <param name="includeGenerated">Whether generated files are allowed.</param>
    /// <returns><c>true</c> if excluded; otherwise <c>false</c>.</returns>
    public static bool ShouldExclude(string fullPath, bool includeGenerated)
    {
        var p = fullPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

        foreach (var d in ExcludedDirs)
        {
            if (p.Contains(d, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        if (!includeGenerated)
        {
            if (p.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase) ||
                p.EndsWith(".g.i.cs", StringComparison.OrdinalIgnoreCase) ||
                p.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase) ||
                p.EndsWith("AssemblyInfo.cs", StringComparison.OrdinalIgnoreCase) ||
                p.EndsWith("GlobalUsings.g.cs", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}