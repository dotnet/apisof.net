using Microsoft.AspNetCore.Components;
using Terrajobst.ApiCatalog;

namespace ApisOfDotNet.Shared;

public enum GlyphKind
{
    Class,
    Constant,
    Database,
    Delegate,
    Enum,
    EnumItem,
    Event,
    ExtensionMethod,
    Field,
    Interface,
    Method,
    Namespace,
    Operator,
    Property,
    Struct
}

public static class GlyphExtensions
{
    public static GlyphKind GetGlyph(this ApiKind kind)
    {
        return kind switch {
            ApiKind.Constructor or
                ApiKind.Destructor or
                ApiKind.PropertyGetter or
                ApiKind.PropertySetter or
                ApiKind.EventAdder or
                ApiKind.EventRemover or
                ApiKind.EventRaiser => GlyphKind.Method,
            ApiKind.Class => GlyphKind.Class,
            ApiKind.Namespace => GlyphKind.Namespace,
            ApiKind.Interface => GlyphKind.Interface,
            ApiKind.Delegate => GlyphKind.Delegate,
            ApiKind.Enum => GlyphKind.Enum,
            ApiKind.Struct => GlyphKind.Struct,
            ApiKind.Constant => GlyphKind.Constant,
            ApiKind.EnumItem => GlyphKind.EnumItem,
            ApiKind.Field => GlyphKind.Field,
            ApiKind.Property => GlyphKind.Property,
            ApiKind.Method => GlyphKind.Method,
            ApiKind.Operator => GlyphKind.Operator,
            ApiKind.Event => GlyphKind.Event,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };
    }

    public static string ToUrl(this GlyphKind kind)
    {
        return kind switch {
            GlyphKind.Database => "/img/Database.svg",
            GlyphKind.Class => "/img/Class.svg",
            GlyphKind.Constant => "/img/Constant.svg",
            GlyphKind.Delegate => "/img/Delegate.svg",
            GlyphKind.Enum => "/img/Enum.svg",
            GlyphKind.EnumItem => "/img/EnumItem.svg",
            GlyphKind.Event => "/img/Event.svg",
            GlyphKind.ExtensionMethod => "/img/ExtensionMethod.svg",
            GlyphKind.Field => "/img/Field.svg",
            GlyphKind.Interface => "/img/Interface.svg",
            GlyphKind.Method => "/img/Method.svg",
            GlyphKind.Namespace => "/img/Namespace.svg",
            GlyphKind.Operator => "/img/Operator.svg",
            GlyphKind.Property => "/img/Property.svg",
            GlyphKind.Struct => "/img/Struct.svg",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };
    }
}

public partial class Glyph
{
    [Parameter]
    public GlyphKind Kind { get; set; }

    public string Url { get; set; }

    protected override void OnParametersSet()
    {
        Url = Kind.ToUrl();
    }
}