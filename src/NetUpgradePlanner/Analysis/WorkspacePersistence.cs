using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NetUpgradePlanner.Analysis;

internal static class WorkspacePersistence
{
    public static async Task<Workspace> LoadAsync(string path)
    {
        using var stream = File.OpenRead(path);
        return await LoadAsync(stream);
    }

    public static async Task<Workspace> LoadAsync(Stream stream)
    {
        var workspaceElement = await XElement.LoadAsync(stream, LoadOptions.None, default);
        var assemblyElements = workspaceElement.Descendants("assembly");

        var configuration = AssemblyConfiguration.Empty;
        var assemblies = new List<AssemblySetEntry>();

        foreach (var assemblyElement in assemblyElements)
        {
            var name = GetRequiredAttribute(assemblyElement, "name");
            var targetFramework = GetRequiredAttribute(assemblyElement, "framework");
            var desiredFramework = GetRequiredAttribute(assemblyElement, "desiredframework");
            var desiredPlatformsText = GetRequiredAttribute(assemblyElement, "desiredPlatforms");
            var desiredPlatforms = PlatformSet.Parse(desiredPlatformsText);

            if (targetFramework.Length == 0)
                targetFramework = null;

            var dependencies = assemblyElement.Descendants("dependency")
                                              .Select(e => GetRequiredAttribute(e, "name"));

            var usedApisElement = GetRequiredElement(assemblyElement, "usedApis");
            var usedApis = usedApisElement.Value.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                                                .Select(Guid.Parse);

            var assembly = new AssemblySetEntry(name, targetFramework, dependencies, usedApis);
            assemblies.Add(assembly);

            configuration = configuration.SetDesiredFramework(assembly, desiredFramework)
                                         .SetDesiredPlatforms(assembly, desiredPlatforms);
        }

        var assemblySet = new AssemblySet(assemblies);

        return new Workspace(assemblySet, configuration, null);

        static string GetRequiredAttribute(XElement element, string attributeName)
        {
            var attribute = element.Attribute(attributeName);
            if (attribute is null)
                throw new FormatException($"element '{element.Name}' is missing attribute '{attributeName}'");

            return attribute.Value;
        }

        static XElement GetRequiredElement(XElement element, string elementName)
        {
            var child = element.Element(elementName);
            if (child is null)
                throw new FormatException($"element '{element.Name}' is missing element '{elementName}'");

            return child;
        }
    }

    public static async Task SaveAsync(Workspace workspace, string path)
    {
        using var stream = File.Create(path);
        await SaveAsync(workspace, stream);
    }

    public static Task SaveAsync(Workspace workspace, Stream stream)
    {
        var workspaceElement = new XElement("workspace",
            new XElement("assemblies",
                workspace.AssemblySet.Entries.Select(a => GetAssemblyElement(workspace, a))
            )
        );

        return workspaceElement.SaveAsync(stream, SaveOptions.None, default);

        static XElement GetAssemblyElement(Workspace workspace, AssemblySetEntry assembly)
        {
            var name = assembly.Name;
            var framework = assembly.TargetFramework ?? string.Empty;
            var desiredFramework = workspace.AssemblyConfiguration.GetDesiredFramework(assembly);
            var desiredPlatforms = workspace.AssemblyConfiguration.GetDesiredPlatforms(assembly).ToString();

            var dependencies = assembly.Dependencies
                                       .Select(d =>
                                            new XElement("dependency",
                                                new XAttribute("name", d)
                                            )
                                       );

            var usedApis = string.Join('\n', assembly.UsedApis);

            return new XElement("assembly",
                new XAttribute("name", name),
                new XAttribute("framework", framework),
                new XAttribute("desiredframework", desiredFramework),
                new XAttribute("desiredPlatforms", desiredPlatforms),
                new XElement("dependencies", dependencies),
                new XElement("usedApis", usedApis)
            );
        }
    }
}
