namespace CSharpRoll.Core;

/// <summary>
/// Provides helper methods to make file lists unique in an OS-appropriate way.
/// </summary>
public static class UniqueFileSet
{
    /// <summary>
    /// Makes file list unique using path comparison rules of the current OS.
    /// </summary>
    public static List<string> MakeUnique(IEnumerable<string> files)
    {
        var comparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        var set = new HashSet<string>(comparer);
        var list = new List<string>();

        foreach (var f in files)
        {
            var full = Path.GetFullPath(f);
            if (set.Add(full))
                list.Add(full);
        }

        return list;
    }
}