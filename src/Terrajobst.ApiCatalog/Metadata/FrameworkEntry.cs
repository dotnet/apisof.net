using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace Terrajobst.ApiCatalog;

public sealed class FrameworkEntry
{
    public static FrameworkEntry Create(string frameworkName, IReadOnlyList<AssemblyEntry> assemblies)
    {
        return new FrameworkEntry(frameworkName, assemblies);
    }

    private FrameworkEntry(string frameworkName, IReadOnlyList<AssemblyEntry> assemblies)
    {
        FrameworkName = frameworkName;
        Assemblies = assemblies;
    }

    public string FrameworkName { get; }
    public IReadOnlyList<AssemblyEntry> Assemblies { get; }

    public void Write(Stream stream)
    {
        var document = new XDocument();
        var root = new XElement("framework", new XAttribute("name", FrameworkName));
        document.Add(root);

        var dictionary = new HashSet<Guid>();

        foreach (var assembly in Assemblies)
        {
            foreach (var api in assembly.AllApis())
            {
                if (dictionary.Add(api.Fingerprint))
                {
                    var apiElement = new XElement("api",
                        new XAttribute("fingerprint", api.Fingerprint.ToString("N")),
                        new XAttribute("kind", (int)api.Kind),
                        new XAttribute("name", api.Name)
                    );
                    root.Add(apiElement);

                    if (api.Parent != null)
                        apiElement.Add(new XAttribute("parent", api.Parent.Fingerprint.ToString("N")));
                }
            }
        }

        foreach (var assembly in Assemblies)
        {
            var assemblyElement = new XElement("assembly",
                new XAttribute("fingerprint", assembly.Fingerprint.ToString("N")),
                new XAttribute("name", assembly.Identity.Name),
                new XAttribute("publicKeyToken", assembly.Identity.GetPublicKeyTokenString()),
                new XAttribute("version", assembly.Identity.Version.ToString())
            );
            root.Add(assemblyElement);

            foreach (var api in assembly.AllApis())
            {
                var syntaxElement = new XElement("syntax", new XAttribute("id", api.Fingerprint.ToString("N")));
                assemblyElement.Add(syntaxElement);
                syntaxElement.Add(api.Syntax);
            }
        }

        document.Save(stream);
    }
}