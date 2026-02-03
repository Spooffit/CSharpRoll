namespace CSharpRoll.Core;

public sealed class RollDiagnostics
{
    public string AnalysisMode { get; }

    // “Solution-level”
    public List<RollDiagnostic> SolutionDiagnostics { get; } = new();

    // csprojPath -> diagnostics
    public Dictionary<string, List<RollDiagnostic>> ProjectDiagnostics { get; }

    // filePath -> line -> diagnostics
    public Dictionary<string, Dictionary<int, List<RollDiagnostic>>> FileDiagnostics { get; }

    public RollDiagnostics()
    {
        var pathComparer = OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;

        ProjectDiagnostics = new Dictionary<string, List<RollDiagnostic>>(pathComparer);
        FileDiagnostics = new Dictionary<string, Dictionary<int, List<RollDiagnostic>>>(pathComparer);
    }
}