namespace CSharpRoll.Core;

/// <summary>
/// Defines options that affect file collection and output.
/// </summary>
public sealed class RollOptions
{
    /// <summary>
    /// Gets a value indicating whether generated files should be included (e.g. *.g.cs, Designer.cs).
    /// </summary>
    public bool IncludeGenerated { get; init; }

    /// <summary>
    /// Gets output format.
    /// </summary>
    public OutputFormat Format { get; init; } = OutputFormat.CSharp;
}