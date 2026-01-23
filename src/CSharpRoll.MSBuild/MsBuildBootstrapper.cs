using Microsoft.Build.Locator;

namespace CSharpRoll.MSBuild;

/// <summary>
/// Bootstraps MSBuild so that evaluation uses the installed MSBuild (SDK/VS).
/// </summary>
public static class MsBuildBootstrapper
{
    /// <summary>
    /// Registers MSBuild instance before any MSBuild APIs are used.
    /// </summary>
    public static void Register()
    {
        if (MSBuildLocator.IsRegistered)
            return;

        var instances = MSBuildLocator.QueryVisualStudioInstances().ToArray();
        if (instances.Length > 0)
        {
            var best = instances.OrderByDescending(i => i.Version).First();
            MSBuildLocator.RegisterInstance(best);
            return;
        }

        MSBuildLocator.RegisterDefaults();
    }
}