using System.Diagnostics;
using System.Xml.Linq;

namespace Terrajobst.ApiCatalog;

public sealed class MissingNetFxFeatureContext
{
    public static MissingNetFxFeatureContext Create() => new();

    private readonly Dictionary<int, MissingNetFxFeature> _mappings = new();
    private readonly HashSet<ApiMatcher> _usedMatchers = new();

    private MissingNetFxFeatureContext()
    {
        Features = LoadFeatures();
    }  
    
    public IReadOnlyList<MissingNetFxFeature> Features { get; }

    private IReadOnlyList<MissingNetFxFeature> LoadFeatures()
    {
        using var stream = GetType().Assembly.GetManifestResourceStream("Terrajobst.ApiCatalog.MissingNetFxFeatures.MissingNetFxFeatures.xml");
        Debug.Assert(stream is not null);
        var document = XDocument.Load(stream);
        return ParseFeatures(document);
    }

    private static IReadOnlyList<MissingNetFxFeature> ParseFeatures(XDocument document)
    {
        var result = new List<MissingNetFxFeature>();

        if (document.Root?.Name.LocalName == "features")
        {
            foreach (var element in document.Root.Elements())
            {
                if (element.Name.LocalName == "feature")
                {
                    var feature = ParseFeature(element);
                    result.Add(feature);
                }
                else
                {
                    throw new Exception($"Unexpected element: <{element.Name}>");
                }
            }
        }
        else
        {
            throw new Exception($"Expected root to be <features> but found: <{document.Root?.Name}>");
        }

        return result;

        static MissingNetFxFeature ParseFeature(XElement featureElement)
        {
            var name = ParseAttribute(featureElement, "name");
            var description = string.Empty;
            var url = string.Empty;
            var appliesTo = Enumerable.Empty<ApiMatcher>();

            foreach (var element in featureElement.Elements())
            {
                switch (element.Name.LocalName)
                {
                    case "description":
                        description = element.Value;
                        break;
                    case "url":
                        url = element.Value;
                        break;
                    case "appliesTo":
                        appliesTo = ParseAppliesTo(element);
                        break;
                    default:
                        throw new Exception($"Unexpected element: <{featureElement.Name}>");
                }
            }

            var feature = new MissingNetFxFeature(name, description, url, appliesTo);
            return feature;
        }

        static List<ApiMatcher> ParseAppliesTo(XElement element)
        {
            var matchers = new List<ApiMatcher>();
            foreach (var matcherElement in element.Elements())
            {
                var matcher = ParseMatcher(matcherElement);
                matchers.Add(matcher);
            }

            return matchers;
        }

        static ApiMatcher ParseMatcher(XElement element)
        {
            switch (element.Name.LocalName)
            {
                case "assembly":
                    return ParseAssemblyMatcher(element);
                case "namespace":
                    return ParseNamespaceMatcher(element);
                case "type":
                    return ParseTypeMatcher(element);
                case "member":
                    return ParseMemberMatcher(element);
                default:
                    throw new Exception($"Unexpected element: <{element.Name}>");
            }
        }

        static ApiMatcher ParseAssemblyMatcher(XElement element)
        {
            var assemblyName = ParseAttribute(element, "name");
            return new AssemblyMatcher(assemblyName);
        }

        static NamespaceMatcher ParseNamespaceMatcher(XElement element)
        {
            var namespaceName = ParseAttribute(element, "name");
            return new NamespaceMatcher(namespaceName);
        }

        static TypeMatcher ParseTypeMatcher(XElement element)
        {
            var namespaceName = ParseAttribute(element, "namespace");
            var typeName = ParseAttribute(element, "name");
            return new TypeMatcher(namespaceName, typeName);
        }

        static MemberMatcher ParseMemberMatcher(XElement element)
        {
            var namespaceName = ParseAttribute(element, "namespace");
            var typeName = ParseAttribute(element, "type");
            var memberName = ParseAttribute(element, "name");
            return new MemberMatcher(namespaceName, typeName, memberName);
        }

        static string ParseAttribute(XElement element, string name)
        {
            var attribute = element.Attribute(name);
            if (attribute is null)
                throw new Exception($"Element <{element.Name}> is missing attribute '{name}'");

            return attribute.Value;
        }
    }

    public MissingNetFxFeature Get(ApiDeclarationModel declaration)
    {
        var apiId = declaration.Api.Id;

        if (!_mappings.TryGetValue(apiId, out var result))
        {
            result = GetNoCache(declaration);
            _mappings.Add(apiId, result);
        }

        return result;
    }

    private MissingNetFxFeature GetNoCache(ApiDeclarationModel declaration)
    {
        var api = declaration.Api;
        var result = LookupFeature(declaration, api);

        // We generally don't want to map individual accessors. So if the accessor
        // isn't mapped, see if the containing event or property is mapped.

        if (result is null && api.Kind.IsAccessor())
        {
            var parent = api.Parent;
            if (parent is not null)
                result = LookupFeature(declaration, parent.Value);
        }

        return result;

        MissingNetFxFeature LookupFeature(ApiDeclarationModel declaration, ApiModel api)
        {
            var assemblyName = declaration.Assembly.Name;
            var namespaceName = api.GetNamespaceName();
            var typeName = api.GetTypeName();
            var memberName = api.GetMemberName();

            foreach (var feature in Features)
            {
                foreach (var matcher in feature.AppliesTo)
                {
                    if (matcher.IsMatch(assemblyName, namespaceName, typeName, memberName))
                    {
                        _usedMatchers.Add(matcher);
                        return feature;
                    }
                }
            }

            return null;
        }
    }

    public IReadOnlyList<ApiMatcher> GetUnusedMatchers()
    {
        return Features.SelectMany(t => t.AppliesTo)
                           .Where(m => !_usedMatchers.Contains(m))
                           .ToArray();
    }
}

