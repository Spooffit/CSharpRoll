using CSharpRoll.Cli.Ui;
using CSharpRoll.Core;
using CSharpRoll.MSBuild;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Text;

namespace CSharpRoll.Cli.Commands;

/// <summary>
/// Roll command implementation.
/// </summary>
public sealed class RollCommand : Command<RollCommandSettings>
{
    /// <inheritdoc />
    public override int Execute(CommandContext context, RollCommandSettings settings, CancellationToken cancellationToken)
    {
        try
        {
            MsBuildBootstrapper.Register();

            var slnPath = ProjectPicker.ResolveSolution(settings.SolutionPath);
            var slnDir = Path.GetDirectoryName(slnPath)!;

            var projects = SolutionReader.ReadCSharpProjects(slnPath);
            if (projects.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]No .csproj projects found in solution.[/]");
                return 2;
            }

            var selected = ProjectPicker.SelectProjects(projects, slnDir, settings.Projects);
            if (selected.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]Nothing selected.[/]");
                return 0;
            }
            
            var options = new RollOptions
            {
                IncludeGenerated = settings.IncludeGenerated,
                Format = settings.Format.Equals("md", StringComparison.OrdinalIgnoreCase)
                    ? OutputFormat.Markdown
                    : OutputFormat.CSharp
            };

            var outputPath = ProjectPicker.ResolveOutput(settings.OutputPath, options.Format, slnDir);

            var filePathComparer = OperatingSystem.IsWindows()
                ? StringComparer.OrdinalIgnoreCase
                : StringComparer.Ordinal;

            var projectBundles = new List<RolledProject>();
            var warnings = new List<string>();

            AnsiConsole.Status().Start("Collecting files...", ctx =>
            {
                foreach (var proj in selected)
                {
                    ctx.Status($"Collecting: [cyan]{Markup.Escape(proj.Name)}[/]");

                    var res = ProjectSourceCollector.Collect(proj.FullPath, options, settings.NoMsbuild);

                    // per-project unique + sorted (global dedupe is in writer)
                    var files = UniqueFileSet.MakeUnique(res.Files);
                    files.Sort(filePathComparer);

                    var csprojRaw = ReadTextRobust(proj.FullPath);

                    projectBundles.Add(new RolledProject(
                        Name: proj.Name,
                        CsprojPath: proj.FullPath,
                        CsprojRaw: csprojRaw,
                        Files: files));

                    warnings.AddRange(res.Warnings.Select(w => $"[{proj.Name}] {w}"));
                }
            });

            if (projectBundles.Count == 0 || projectBundles.All(p => p.Files.Count == 0))
            {
                AnsiConsole.MarkupLine("[red]No C# files collected.[/]");
                return 3;
            }

            // total unique (for console output)
            var totalUnique = new HashSet<string>(filePathComparer);
            foreach (var p in projectBundles)
            foreach (var f in p.Files)
                totalUnique.Add(f);

            var rolledFiles = new HashSet<string>(filePathComparer);
            foreach (var p in projectBundles)
            foreach (var f in p.Files)
                rolledFiles.Add(f);

            RollDiagnostics? diags = null;

            if (!settings.NoMsbuild)
            {
                AnsiConsole.Status().Start("Running analyzers...", _ =>
                {
                    diags = AnalyzerDiagnosticsCollector.Collect(
                        solutionPath: slnPath,
                        selectedProjectCsprojPaths: projectBundles.Select(p => p.CsprojPath).ToList(),
                        rolledFiles: rolledFiles,
                        includeGenerated: settings.IncludeGenerated,
                        cancellationToken: cancellationToken);
                });
            }
            else
            {
                diags = new RollDiagnostics();
                if (settings.NoMsbuild)
                {
                    diags.SolutionDiagnostics.Add(new RollDiagnostic(
                        RollDiagnosticSeverity.Warning,
                        "ANALYZERS",
                        "Analyzer collection skipped because --no-msbuild is enabled.",
                        null, null, null));
                }
            }
            
            RollWriter.Write(outputPath, projectBundles, slnDir, options, slnPath, diags);

            AnsiConsole.MarkupLine($"[green]Done.[/] Output: [cyan]{Markup.Escape(outputPath)}[/]");
            AnsiConsole.MarkupLine($"Projects: [cyan]{projectBundles.Count}[/], Files: [cyan]{totalUnique.Count}[/]");

            if (warnings.Count > 0)
            {
                AnsiConsole.MarkupLine("[yellow]Warnings:[/]");
                foreach (var w in warnings.Distinct())
                    AnsiConsole.MarkupLine($"  [grey]- {Markup.Escape(w)}[/]");
            }

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine("[red]Fatal error:[/]");
            AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
            return 1;
        }
    }

    private static string ReadTextRobust(string path)
    {
        try
        {
            return File.ReadAllText(path, new UTF8Encoding(false, true));
        }
        catch
        {
            return File.ReadAllText(path);
        }
    }
}
