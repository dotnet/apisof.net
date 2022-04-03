using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace ApiCatalog.Metadata;

public sealed class PackageEntry
{
    public static PackageEntry Create(string id, string version, IReadOnlyList<FrameworkEntry> entries)
    {
        return new PackageEntry(id, version, entries);
    }

    private PackageEntry(string id, string version, IReadOnlyList<FrameworkEntry> entries)
    {
        Fingerprint = CatalogExtensions.GetCatalogGuid(id, version);
        Id = id;
        Version = version;
        Entries = entries;
    }

    public Guid Fingerprint { get; }
    public string Id { get; }
    public string Version { get; }
    public IReadOnlyList<FrameworkEntry> Entries { get; }

    public void Write(Stream stream)
    {
        var document = new XDocument();
        var root = new XElement("package",
            new XAttribute("fingerprint", Fingerprint),
            new XAttribute("id", Id),
            new XAttribute("name", Version)
        );
        document.Add(root);

        var dictionary = new HashSet<Guid>();

        foreach (var fx in Entries)
        {
            foreach (var assembly in fx.Assemblies)
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
        }

        foreach (var fx in Entries)
        {
            foreach (var assembly in fx.Assemblies)
            {
                var assemblyElement = new XElement("assembly",
                    new XAttribute("fx", fx.FrameworkName),
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
        }

        document.Save(stream);
    }
}