namespace CSharpRoll.Core;

/// <summary>
/// Defines output format for the rolled content.
/// </summary>
public enum OutputFormat
{
    /// <summary>
    /// Produces a plain .cs file with file separators.
    /// </summary>
    CSharp,

    /// <summary>
    /// Produces a Markdown document with fenced csharp code blocks.
    /// </summary>
    Markdown
}