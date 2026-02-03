namespace CSharpRoll.Core;

public sealed record RollDiagnostic(
    RollDiagnosticSeverity Severity,
    string Id,
    string Message,
    string? FilePath,
    int? Line,
    int? Column);