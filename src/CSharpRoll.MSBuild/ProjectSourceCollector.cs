using CSharpRoll.Core;
using Microsoft.Build.Evaluation;

namespace CSharpRoll.MSBuild;

/// <summary>
/// Collects C# source files from a .csproj.
/// </summary>
public static class ProjectSourceCollector
{
    /// <summary>
    /// Collects .cs files via MSBuild Compile items with a filesystem fallback.
/// </summary>
    public static ProjectSourceCollectorResult Collect(string csprojPath, RollOptions options, bool noMsbuild)
    {
        var result = new ProjectSourceCollectorResult();

        if (!File.Exists(csprojPath))
        {
            result.Warnings.Add($"Project not found: {csprojPath}");
            return result;
        }

        if (!noMsbuild)
        {
            try
            {
                foreach (var f in CollectViaMsbuild(csprojPath, options))
                    result.Files.Add(f);

                if (result.Files.Count > 0)
                    return result;
            }
            catch (Exception ex)
            {
                result.Warnings.Add($"MSBuild evaluation failed, fallback to filesystem. {ex.GetType().Name}: {ex.Message}");
            }
        }

        foreach (var f in CollectViaFilesystem(csprojPath, options))
            result.Files.Add(f);

        return result;
    }

    private static IEnumerable<string> CollectViaMsbuild(string csprojPath, RollOptions options)
    {
        var projectDir = Path.GetDirectoryName(csprojPath)!;

        var globalProps = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // TODO: Analyzer language
            ["PreferredUILang"] = "en-US",
            ["DesignTimeBuild"] = "true",
            ["BuildProjectReferences"] = "false",
            ["SkipCompilerExecution"] = "true",
            ["ProvideCommandLineArgs"] = "true",
        };

        using var collection = new ProjectCollection(globalProps);
        var proj = collection.LoadProject(csprojPath);

        foreach (var item in proj.GetItems("Compile"))
        {
            var include = item.EvaluatedInclude;
            if (string.IsNullOrWhiteSpace(include))
                continue;

            if (include.Contains('*') || include.Contains('?'))
                continue;

            var full = Path.GetFullPath(Path.Combine(projectDir, include));

            if (!full.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                continue;

            if (PathFilter.ShouldExclude(full, options.IncludeGenerated))
                continue;

            yield return full;
        }
    }

    private static IEnumerable<string> CollectViaFilesystem(string csprojPath, RollOptions options)
    {
        var projectDir = Path.GetDirectoryName(csprojPath)!;
        var files = Directory.GetFiles(projectDir, "*.cs", SearchOption.AllDirectories);

        foreach (var f in files)
        {
            var full = Path.GetFullPath(f);

            if (PathFilter.ShouldExclude(full, options.IncludeGenerated))
                continue;

            yield return full;
        }
    }
}