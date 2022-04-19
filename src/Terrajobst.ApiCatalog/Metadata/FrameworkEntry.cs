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
        XmlEntryFormat.WriteFrameworkEntry(stream, this);
    }

    public XDocument ToDocument()
    {
        using var stream = new MemoryStream();
        Write(stream);
        stream.Position = 0;
        return XDocument.Load(stream);
    }
}