using System.Diagnostics;

using NuGet.Frameworks;

namespace Terrajobst.ApiCatalog.Features;

public sealed class TargetFrameworkHierarchy
{
    public static TargetFrameworkHierarchy Empty { get; } = new(Array.Empty<TargetFrameworkNode>());

    public static IEnumerable<NuGetFramework> GetAncestorsAndSelf(NuGetFramework framework)
    {
        ThrowIfNull(framework);

        yield return framework;

        if (framework.HasPlatform)
        {
            if (framework.PlatformVersion != FrameworkConstants.EmptyVersion)
                yield return new NuGetFramework(framework.Framework, framework.Version, framework.Platform, FrameworkConstants.EmptyVersion);

            yield return new NuGetFramework(framework.Framework, framework.Version);
        }

        var frameworkFamily = new NuGetFramework(framework.Framework, FrameworkConstants.EmptyVersion);
        yield return frameworkFamily;
    }

    private TargetFrameworkHierarchy(IEnumerable<TargetFrameworkNode> roots)
    {
        ThrowIfNull(roots);

        Roots = roots.ToArray();
    }

    public static TargetFrameworkHierarchy Create(ApiCatalogModel catalog)
    {
        ThrowIfNull(catalog);

        var nodeByFramework = new Dictionary<NuGetFramework, Node>();
        var roots = new List<Node>();

        foreach (var framework in catalog.Frameworks)
        {
            if (!framework.NuGetFramework.IsRelevant())
                continue;

            Node? child = null;
            var lastAncestorExisted = false;

            foreach (var ancestor in GetAncestorsAndSelf(framework.NuGetFramework))
            {
                if (nodeByFramework.TryGetValue(ancestor, out var node))
                {
                    lastAncestorExisted = true;
                }
                else
                {
                    node = new Node(ancestor);
                    nodeByFramework.Add(ancestor, node);
                    lastAncestorExisted = false;
                }

                if (child is not null && !node.Children.Contains(child))
                    node.Children.Add(child);

                child = node;
            }

            if (!lastAncestorExisted)
            {
                Debug.Assert(child is not null);
                roots.Add(child);
            }
        }

        return new TargetFrameworkHierarchy(CreateChildren(null, roots));
    }

    public IReadOnlyList<TargetFrameworkNode> Roots { get; }

    private static IEnumerable<TargetFrameworkNode> CreateChildren(TargetFrameworkNode? parent, IEnumerable<Node> children)
    {
        return children.Select(c => new TargetFrameworkNode(parent, c.Framework, p => CreateChildren(p, c.Children)))
                       .OrderBy(n => n.Framework.Framework)
                       .ThenBy(n => n.Framework.Version)
                       .ThenBy(n => n.Framework.HasPlatform)
                       .ThenBy(n => n.Framework.Platform)
                       .ThenBy(n => n.Framework.Version);
    }

    private sealed class Node
    {
        public Node(NuGetFramework framework)
        {
            Framework = framework;
        }

        public NuGetFramework Framework { get; }
        public List<Node> Children { get; } = new();
    }
}