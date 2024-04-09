using NuGet.Frameworks;

namespace Terrajobst.ApiCatalog;

public sealed class DiffWriter
{
    private readonly ApiCatalogModel _catalog;
    private readonly NuGetFramework _left;
    private readonly NuGetFramework _right;
    private readonly bool _excludeUnchanged;

    public DiffWriter(ApiCatalogModel catalog, NuGetFramework left, NuGetFramework right, bool excludeUnchanged)
    {
        ThrowIfNull(catalog);
        ThrowIfNull(left);
        ThrowIfNull(right);

        _catalog = catalog;
        _left = left;
        _right = right;
        _excludeUnchanged = excludeUnchanged;
    }

    public async Task WriteToAsync(TextWriter writer)
    {
        ThrowIfNull(writer);

        foreach (var root in _catalog.RootApis.Order())
            await WriteToAsync(root, writer);
    }

    public async Task WriteToAsync(Stream stream)
    {
        ThrowIfNull(stream);

        await using var writer = new StreamWriter(stream, leaveOpen: true);
        await WriteToAsync(writer);
    }

    private async Task WriteToAsync(ApiModel api, TextWriter writer, int indent = 0)
    {
        if (api.Kind.IsAccessor())
            return;

        var defLeft = api.GetDefinition(_left);
        var defRight = api.GetDefinition(_right);

        if (defLeft is null && defRight is null)
            return;

        if (_excludeUnchanged && !api.ContainsDifferences(_left, _right))
            return;

        var blockDiffMarker = " ";

        if (defLeft is null)
        {
            blockDiffMarker = "+";
            await WriteDeclarationAsync("+", defRight!.Value, writer, indent);
        }
        else if (defRight is null)
        {
            blockDiffMarker = "-";
            await WriteDeclarationAsync("-", defLeft.Value, writer, indent);
        }
        else if (defLeft.Value.MarkupId == defRight.Value.MarkupId)
        {
            await WriteDeclarationAsync(" ", defRight.Value, writer, indent);
        }
        else
        {
            await WriteDeclarationAsync("-", defLeft.Value, writer, indent);
            await WriteDeclarationAsync("+", defRight.Value, writer, indent);
        }

        if (api.CanHaveChildren())
        {
            await WriteDiffMarkerAndIndentAsync(blockDiffMarker, writer, indent);
            await writer.WriteLineAsync("{");

            foreach (var child in api.Children.Order())
                await WriteToAsync(child, writer, indent + 1);

            await WriteDiffMarkerAndIndentAsync(blockDiffMarker, writer, indent);
            await writer.WriteLineAsync("}");
        }
    }

    private static async Task WriteDeclarationAsync(string diffMarker, ApiDeclarationModel declaration, TextWriter writer, int indent)
    {
        var markup = declaration.GetMyMarkup();

        await WriteDiffMarkerAndIndentAsync(diffMarker, writer, indent);

        foreach (var token in markup.Tokens)
        {
            if (token.Kind != MarkupTokenKind.LineBreak)
            {
                await writer.WriteAsync(token.Text);
            }
            else
            {
                await writer.WriteLineAsync();
                await WriteDiffMarkerAndIndentAsync(diffMarker, writer, indent);
            }
        }

        await writer.WriteLineAsync();
    }

    private static async Task WriteDiffMarkerAndIndentAsync(string diffMarker, TextWriter writer, int indent)
    {
        await writer.WriteAsync(diffMarker);
        for (var i = 0; i < indent; i++)
            await writer.WriteAsync("    ");
    }
}