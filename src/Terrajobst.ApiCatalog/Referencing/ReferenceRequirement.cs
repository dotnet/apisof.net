using System.CodeDom.Compiler;

namespace Terrajobst.ApiCatalog;

public abstract partial class ReferenceRequirement
{
    public abstract void WriteTo(IndentedTextWriter writer);

    public sealed override string ToString()
    {
        using var stringWriter = new StringWriter();

        using (var indentedWriter = new IndentedTextWriter(stringWriter))
            WriteTo(indentedWriter);

        return stringWriter.ToString();
    }
}
