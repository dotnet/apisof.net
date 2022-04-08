using System.Xml.Linq;

namespace Terrajobst.ApiCatalog;

internal static class XmlEntryFormat
{
    public static void WriteFrameworkEntry(Stream stream, FrameworkEntry frameworkEntry)
    {
        var document = new XDocument();
        var root = new XElement("framework", new XAttribute("name", frameworkEntry.FrameworkName));
        document.Add(root);

        var dictionary = new HashSet<Guid>();

        foreach (var assembly in frameworkEntry.Assemblies)
        {
            foreach (var api in assembly.AllApis())
            {
                if (dictionary.Add(api.Fingerprint))
                    AddApi(root, api);
            }
        }

        foreach (var assembly in frameworkEntry.Assemblies)
            AddAssembly(root, assembly);

        document.Save(stream);
    }

    public static void WritePackageEntry(Stream stream, PackageEntry packageEntry)
    {
        var document = new XDocument();
        var root = new XElement("package",
            new XAttribute("fingerprint", packageEntry.Fingerprint),
            new XAttribute("id", packageEntry.Id),
            new XAttribute("name", packageEntry.Version)
        );
        document.Add(root);

        var dictionary = new HashSet<Guid>();

        foreach (var fx in packageEntry.Entries)
        {
            foreach (var assembly in fx.Assemblies)
            {
                foreach (var api in assembly.AllApis())
                {
                    if (dictionary.Add(api.Fingerprint))
                        AddApi(root, api);
                }
            }
        }

        foreach (var fx in packageEntry.Entries)
        {
            foreach (var assembly in fx.Assemblies)
                AddAssembly(root, assembly, fx.FrameworkName);
        }

        document.Save(stream);
    }

    private static void AddAssembly(XContainer parent, AssemblyEntry assembly, string frameworkName = null)
    {
        var assemblyElement = new XElement("assembly",
            frameworkName is null ? null : new XAttribute("fx", frameworkName),
            new XAttribute("fingerprint", assembly.Fingerprint.ToString("N")),
            new XAttribute("name", assembly.Identity.Name),
            new XAttribute("publicKeyToken", assembly.Identity.GetPublicKeyTokenString()),
            new XAttribute("version", assembly.Identity.Version.ToString())
        );
        parent.Add(assemblyElement);

        foreach (var api in assembly.AllApis())
        {
            var fingerprint = api.Fingerprint.ToString("N");
            
            var syntaxElement = new XElement("syntax", new XAttribute("id", fingerprint));
            assemblyElement.Add(syntaxElement);
            syntaxElement.Add(api.Syntax);
        }
    }

    private static void AddApi(XContainer parent, ApiEntry api)
    {
        var apiElement = new XElement("api",
            new XAttribute("fingerprint", api.Fingerprint.ToString("N")),
            new XAttribute("kind", (int) api.Kind),
            new XAttribute("name", api.Name)
        );
        parent.Add(apiElement);

        if (api.Parent != null)
            apiElement.Add(new XAttribute("parent", api.Parent.Fingerprint.ToString("N")));
    }
}