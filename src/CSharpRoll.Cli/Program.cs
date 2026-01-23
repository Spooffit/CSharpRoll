using CSharpRoll.Cli.Commands;
using Spectre.Console.Cli;

namespace CSharpRoll.Cli;

/// <summary>
/// Application entry point.
/// </summary>
internal static class Program
{
    public static int Main(string[] args)
    {
        var app = new CommandApp();
        app.SetDefaultCommand<RollCommand>();
        app.Configure(config =>
        {
            config.SetApplicationName("csharproll");
            config.AddCommand<RollCommand>("roll")
                .WithDescription("Rolls selected projects from a .sln into a single output file.");
        });

        return app.Run(args);
    }
}
