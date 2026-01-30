using CSharpRoll.Core;
using Microsoft.Build.Evaluation;

namespace CSharpRoll.Cli;

internal static class ProjectSnapshotReader
{
    public static RolledProject Read(
        ProjectCollection projectCollection,
        string projectName,
        string csprojPath,
        IReadOnlyList<string> files)
    {
        // MSBuild is case-insensitive for names; don't tie this to OS filesystem comparer.
        var nameComparer = StringComparer.OrdinalIgnoreCase;

        var csprojRaw = File.ReadAllText(csprojPath);

        var p = projectCollection.LoadProject(csprojPath);

        // Effective properties: "last wins" while preserving evaluation order.
        var props = new Dictionary<string, string>(nameComparer);
        foreach (var ep in p.AllEvaluatedProperties)
            props[ep.Name] = ep.EvaluatedValue;

        // ItemDefinitions: effective default metadata per item type.
        var itemDefs = new Dictionary<string, IReadOnlyDictionary<string, string>>(nameComparer);
        foreach (var def in p.ItemDefinitions)
        {
            var md = new Dictionary<string, string>(nameComparer);
            foreach (var m in def.Value.Metadata)
                md[m.Name] = m.EvaluatedValue;

            itemDefs[def.Key] = md;
        }

        // Items + custom metadata
        var items = new List<RolledItem>();
        foreach (var it in p.AllEvaluatedItems)
        {
            var md = new Dictionary<string, string>(nameComparer);
            foreach (var m in it.Metadata)
                md[m.Name] = m.EvaluatedValue;

            items.Add(new RolledItem(it.ItemType, it.EvaluatedInclude, md));
        }

        projectCollection.UnloadProject(p);

        return new RolledProject(
            Name: projectName,
            CsprojPath: csprojPath,
            CsprojRaw: csprojRaw,
            Files: files);
    }
}