using Spectre.Console;

namespace CSharpRoll.Cli.Ui;

public static class AnalysisModePicker
{
    private static readonly string[] Allowed =
    {
        // Default первым, чтобы Enter = Default (потому что SelectionPrompt не умеет default choice нормально)
        "Default",
        "Recommended",
        "Minimum",
        "All",
        "None",
    };

    public static string Select(string? explicitMode)
    {
        if (!string.IsNullOrWhiteSpace(explicitMode))
            return NormalizeOrThrow(explicitMode);

        var prompt = new SelectionPrompt<string>()
            .Title("Select analyzer [green]<AnalysisMode>[/]:")
            .PageSize(10)
            .AddChoices(Allowed);

        return AnsiConsole.Prompt(prompt);
    }

    private static string NormalizeOrThrow(string mode)
    {
        var normalized = mode.Trim();

        foreach (var a in Allowed)
        {
            if (string.Equals(a, normalized, StringComparison.OrdinalIgnoreCase))
                return a;
        }

        throw new ArgumentException(
            $"Invalid --analysis-mode '{mode}'. Allowed: {string.Join(", ", Allowed)}");
    }
}