using Spectre.Console.Cli;
using System.ComponentModel;

namespace CSharpRoll.Cli.Commands;

/// <summary>
/// CLI settings for the roll command.
/// </summary>
public sealed class RollCommandSettings : CommandSettings
{
    /// <summary>
    /// Path to directory where search .sln / .slnx files
    /// </summary>
    [CommandOption("-d|--dir <DIR>")]
    [Description("Path to directory where search .sln / .slnx files.")]
    public string? DirectoryPath { get; init; }
    
    /// <summary>
    /// Gets the solution path.
    /// </summary>
    [CommandOption("-s|--sln <SLN>")]
    [Description("Path to .sln / .slnx. If omitted, the tool searches current directory.")]
    public string? SolutionPath { get; init; }

    /// <summary>
    /// Gets the comma-separated project paths (relative to solution dir or absolute).
    /// </summary>
    [CommandOption("-p|--projects <PROJECTS>")]
    [Description("Comma-separated csproj paths to select (skips interactive prompt).")]
    public string? Projects { get; init; }

    /// <summary>
    /// Gets output file path.
    /// </summary>
    [CommandOption("-o|--out <OUT>")]
    [Description("Output file path. Default: CSharpRoll.cs / CSharpRoll.md in solution directory.")]
    public string? OutputPath { get; init; }
    
    /// <summary>
    /// Gets output format: cs or md.
    /// </summary>
    [CommandOption("-f|--format <FORMAT>")]
    [DefaultValue("cs")]
    public string Format { get; init; } = "cs";

    /// <summary>
    /// Gets a value indicating whether generated files are included.
    /// </summary>
    [CommandOption("--include-generated")]
    public bool IncludeGenerated { get; init; }

    /// <summary>
    /// Gets a value indicating whether MSBuild evaluation should be skipped.
    /// </summary>
    [CommandOption("--no-msbuild")]
    public bool NoMsbuild { get; init; }
}