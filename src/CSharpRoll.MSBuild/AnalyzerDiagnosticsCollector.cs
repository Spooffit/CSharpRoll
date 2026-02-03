using System.Collections.Immutable;
using System.Globalization;
using CSharpRoll.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.MSBuild;

namespace CSharpRoll.MSBuild;

public static class AnalyzerDiagnosticsCollector
{
    public static RollDiagnostics Collect(
        string solutionPath,
        IReadOnlyList<string> selectedProjectCsprojPaths,
        IReadOnlyCollection<string> rolledFiles, // full paths of files that will be written
        bool includeGenerated,
        CancellationToken cancellationToken)
    {
        var diagnostics = new RollDiagnostics();

        var pathComparer = OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;

        var selectedProjects = selectedProjectCsprojPaths
            .Select(Path.GetFullPath)
            .ToHashSet(pathComparer);

        var rolledFilesSet = rolledFiles
            .Select(Path.GetFullPath)
            .ToHashSet(pathComparer);

        var globalProps = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["AnalysisMode"] = "Recommended",
            // TODO: Analyzer language
            ["PreferredUILang"] = "en-US",
            ["DesignTimeBuild"] = "true",
            ["BuildProjectReferences"] = "false"
        };
        
        var seen = new HashSet<string>(StringComparer.Ordinal);

        using var workspace = MSBuildWorkspace.Create(globalProps);

        using var _failedReg = workspace.RegisterWorkspaceFailedHandler(e =>
        {
            var sev = e.Diagnostic.Kind == WorkspaceDiagnosticKind.Failure
                ? RollDiagnosticSeverity.Error
                : RollDiagnosticSeverity.Warning;

            AddSolution(diagnostics, seen, new RollDiagnostic(
                sev,
                "WORKSPACE",
                e.Diagnostic.Message,
                null, null, null));
        });

        Solution sln;
        try
        {
            sln = workspace.OpenSolutionAsync(solutionPath, null, cancellationToken)
                .GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            AddSolution(diagnostics, seen, new RollDiagnostic(
                RollDiagnosticSeverity.Error,
                "WORKSPACE",
                $"Failed to open solution: {ex.GetType().Name}: {ex.Message}",
                null, null, null));

            return diagnostics;
        }

        foreach (var project in sln.Projects)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var projectFile = project.FilePath;
            if (string.IsNullOrWhiteSpace(projectFile))
                continue;

            var fullProjectFile = Path.GetFullPath(projectFile);
            if (!selectedProjects.Contains(fullProjectFile))
                continue;

            Compilation? compilation;
            try
            {
                compilation = project.GetCompilationAsync(cancellationToken)
                    .GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                AddProject(diagnostics, seen, fullProjectFile, new RollDiagnostic(
                    RollDiagnosticSeverity.Error,
                    "COMPILATION",
                    $"Failed to get compilation: {ex.GetType().Name}: {ex.Message}",
                    null, null, null));
                continue;
            }

            if (compilation is null)
            {
                AddProject(diagnostics, seen, fullProjectFile, new RollDiagnostic(
                    RollDiagnosticSeverity.Error,
                    "COMPILATION",
                    "Compilation is null (project could not be evaluated).",
                    null, null, null));
                continue;
            }
            
            var compilerDiagnostics = compilation.GetDiagnostics(cancellationToken);

            // 2) analyzer diagnostics
            var analyzers = project.AnalyzerReferences
                .SelectMany(r => r.GetAnalyzers(project.Language))
                .ToImmutableArray();

            var analyzerDiagnostics = Array.Empty<Diagnostic>();
            try
            {
                if (!analyzers.IsDefaultOrEmpty)
                {
                    var withAnalyzers =
                        compilation.WithAnalyzers(analyzers, project.AnalyzerOptions, cancellationToken);
                    analyzerDiagnostics = withAnalyzers.GetAnalyzerDiagnosticsAsync(cancellationToken)
                        .GetAwaiter().GetResult()
                        .ToArray();
                }
            }
            catch (Exception ex)
            {
                AddProject(diagnostics, seen, fullProjectFile, new RollDiagnostic(
                    RollDiagnosticSeverity.Warning,
                    "ANALYZERS",
                    $"Analyzer run failed: {ex.GetType().Name}: {ex.Message}",
                    null, null, null));
            }

            foreach (var d in compilerDiagnostics.Concat(analyzerDiagnostics))
            {
                if (d.IsSuppressed)
                    continue;

                if (d.Severity is not (DiagnosticSeverity.Warning or DiagnosticSeverity.Error))
                    continue;

                var sev = d.Severity == DiagnosticSeverity.Error
                    ? RollDiagnosticSeverity.Error
                    : RollDiagnosticSeverity.Warning;
                
                // TODO: Analyzer language
                var en = CultureInfo.GetCultureInfo("en-US");
                var msg = d.GetMessage(en).Replace("\r", " ").Replace("\n", " ").Trim();
                
                if (TryGetSourceLocation(d, rolledFilesSet, out var file, out var line, out var col))
                {
                    if (!includeGenerated && PathFilter.ShouldExclude(file!, false))
                        continue;

                    AddFileLine(diagnostics, seen, file!, line!.Value, new RollDiagnostic(
                        sev, d.Id, msg, file, line, col));
                }
                else
                {
                    var (pf, pl, pc) = GetAnyLocation(d);
                    if (pf is not null)
                    {
                        var full = Path.GetFullPath(pf);
                        
                        if (!includeGenerated && PathFilter.ShouldExclude(full, false))
                            continue;

                        AddProject(diagnostics, seen, fullProjectFile, new RollDiagnostic(
                            sev, d.Id, msg, full, pl, pc));
                    }
                    else
                    {
                        AddProject(diagnostics, seen, fullProjectFile, new RollDiagnostic(
                            sev, d.Id, msg, null, null, null));
                    }
                }
            }
        }

        return diagnostics;

        static bool TryGetSourceLocation(
            Diagnostic d,
            HashSet<string> rolledFiles,
            out string? filePath,
            out int? line,
            out int? col)
        {
            filePath = null;
            line = null;
            col = null;

            if (!d.Location.IsInSource)
                return false;

            var mapped = d.Location.GetMappedLineSpan();
            if (TryUseSpan(mapped, rolledFiles, out filePath, out line, out col))
                return true;

            var span = d.Location.GetLineSpan();
            return TryUseSpan(span, rolledFiles, out filePath, out line, out col);
        }

        static bool TryUseSpan(
            FileLinePositionSpan span,
            HashSet<string> rolledFiles,
            out string? filePath,
            out int? line,
            out int? col)
        {
            filePath = null;
            line = null;
            col = null;

            var p = span.Path;
            if (string.IsNullOrWhiteSpace(p))
                return false;

            var full = Path.GetFullPath(p);
            if (!rolledFiles.Contains(full))
                return false;

            filePath = full;
            line = span.StartLinePosition.Line + 1;
            col = span.StartLinePosition.Character + 1;
            return true;
        }

        static (string? file, int? line, int? col) GetAnyLocation(Diagnostic d)
        {
            if (d.Location == Location.None)
                return (null, null, null);

            var span = d.Location.GetLineSpan();
            if (string.IsNullOrWhiteSpace(span.Path))
                return (null, null, null);

            return (
                span.Path,
                span.StartLinePosition.Line + 1,
                span.StartLinePosition.Character + 1
            );
        }

        static void AddSolution(RollDiagnostics diags, HashSet<string> seen, RollDiagnostic d)
        {
            if (!seen.Add(Key(d))) return;
            diags.SolutionDiagnostics.Add(d);
        }

        static void AddProject(RollDiagnostics diags, HashSet<string> seen, string csproj, RollDiagnostic d)
        {
            if (!seen.Add(Key(d))) return;
            if (!diags.ProjectDiagnostics.TryGetValue(csproj, out var list))
                diags.ProjectDiagnostics[csproj] = list = new List<RollDiagnostic>();
            list.Add(d);
        }

        static void AddFileLine(RollDiagnostics diags, HashSet<string> seen, string file, int line, RollDiagnostic d)
        {
            if (!seen.Add(Key(d))) return;

            if (!diags.FileDiagnostics.TryGetValue(file, out var byLine))
                diags.FileDiagnostics[file] = byLine = new Dictionary<int, List<RollDiagnostic>>();

            if (!byLine.TryGetValue(line, out var list))
                byLine[line] = list = new List<RollDiagnostic>();

            list.Add(d);
        }

        static string Key(RollDiagnostic d)
        {
            return $"{d.Severity}|{d.Id}|{d.FilePath}|{d.Line}|{d.Column}|{d.Message}";
        }
    }
}