using CSharpRoll.Cli.Ui;
using CSharpRoll.Core;
using CSharpRoll.MSBuild;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CSharpRoll.Cli.Commands;

/// <summary>
///     Roll command implementation.
/// </summary>
public sealed class RollCommand : Command<RollCommandSettings>
{
    /// <inheritdoc />
    public override int Execute(CommandContext context, RollCommandSettings settings,
        CancellationToken cancellationToken)
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

            var allFiles = new List<string>();
            var warnings = new List<string>();

            AnsiConsole.Status().Start("Collecting files...", ctx =>
            {
                foreach (var proj in selected)
                {
                    ctx.Status($"Collecting: [cyan]{Markup.Escape(proj.Name)}[/]");

                    var res = ProjectSourceCollector.Collect(proj.FullPath, options, settings.NoMsbuild);
                    allFiles.AddRange(res.Files);
                    warnings.AddRange(res.Warnings.Select(w => $"[{proj.Name}] {w}"));
                }
            });

            var unique = UniqueFileSet.MakeUnique(allFiles);
            unique.Sort(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);

            if (unique.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]No C# files collected.[/]");
                return 3;
            }

            RollWriter.Write(
                outputPath,
                unique,
                slnDir,
                selected.Select(p => p.Name).ToList(),
                options);

            AnsiConsole.MarkupLine($"[green]Done.[/] Output: [cyan]{Markup.Escape(outputPath)}[/]");
            AnsiConsole.MarkupLine($"Projects: [cyan]{selected.Count}[/], Files: [cyan]{unique.Count}[/]");

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
}