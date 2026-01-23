using CSharpRoll.Core;
using CSharpRoll.MSBuild;
using Spectre.Console;

namespace CSharpRoll.Cli.Ui;

/// <summary>
///     Provides interactive prompts for selecting solutions and projects.
/// </summary>
public static class ProjectPicker
{
    /// <summary>
    ///     Resolves solution path from an explicit path or by scanning current directory.
    /// </summary>
    public static string ResolveSolution(string? explicitSlnPath)
    {
        if (!string.IsNullOrWhiteSpace(explicitSlnPath))
        {
            var full = Path.GetFullPath(explicitSlnPath);
            if (!File.Exists(full))
                throw new FileNotFoundException($"Solution not found: {full}");
            if (!full.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Only .sln files are supported.");
            return full;
        }

        var cwd = Directory.GetCurrentDirectory();
        var slns = Directory.GetFiles(cwd, "*.sln", SearchOption.TopDirectoryOnly)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (slns.Count == 0)
            throw new FileNotFoundException($"No .sln found in {cwd}. Use --sln <path>.");

        if (slns.Count == 1)
            return Path.GetFullPath(slns[0]);

        var prompt = new SelectionPrompt<string>()
            .Title("Multiple solutions found. Choose one:")
            .PageSize(12)
            .AddChoices(slns.Select(Path.GetFileName)!);

        var chosenName = AnsiConsole.Prompt(prompt);
        var chosenPath = slns.First(x =>
            string.Equals(Path.GetFileName(x), chosenName, StringComparison.OrdinalIgnoreCase));
        return Path.GetFullPath(chosenPath);
    }

    /// <summary>
    ///     Selects projects either via explicit paths or interactive UI.
    /// </summary>
    public static List<SolutionProjectInfo> SelectProjects(
        List<SolutionProjectInfo> projects,
        string solutionDir,
        string? projectsCsv)
    {
        if (!string.IsNullOrWhiteSpace(projectsCsv))
        {
            var requested = projectsCsv
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(p =>
                    Path.IsPathRooted(p) ? Path.GetFullPath(p) : Path.GetFullPath(Path.Combine(solutionDir, p)))
                .ToHashSet(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);

            var selected = projects.Where(p => requested.Contains(Path.GetFullPath(p.FullPath))).ToList();
            if (selected.Count == 0)
                throw new InvalidOperationException("No projects matched --projects input.");

            return selected;
        }

        var prompt = new MultiSelectionPrompt<SolutionProjectInfo>()
            .Title("Select one or more [green].csproj[/] to roll:")
            .NotRequired()
            .PageSize(15)
            .InstructionsText("[grey](Space to toggle, Enter to accept, Ctrl+A select all)[/]")
            .UseConverter(p => $"{p.Name} [grey]({p.RelativePath})[/]");

        prompt.AddChoices(projects);
        return AnsiConsole.Prompt(prompt).ToList();
    }

    /// <summary>
    ///     Resolves output path.
    /// </summary>
    public static string ResolveOutput(string? outputPath, OutputFormat format, string solutionDir)
    {
        if (!string.IsNullOrWhiteSpace(outputPath))
            return Path.GetFullPath(Path.IsPathRooted(outputPath) ? outputPath : Path.Combine(solutionDir, outputPath));

        var name = format == OutputFormat.Markdown ? "CSharpRoll.md" : "CSharpRoll.cs";
        return Path.GetFullPath(Path.Combine(solutionDir, name));
    }
}