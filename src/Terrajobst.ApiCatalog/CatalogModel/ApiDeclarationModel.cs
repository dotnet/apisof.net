namespace Terrajobst.ApiCatalog;

public readonly struct ApiDeclarationModel : IEquatable<ApiDeclarationModel>
{
    private readonly ApiModel _api;
    private readonly int _offset;

    internal ApiDeclarationModel(ApiModel api, int offset)
    {
        ApiCatalogSchema.EnsureValidBlobOffset(api.Catalog, offset);

        _api = api;
        _offset = offset;
    }

    public ApiCatalogModel Catalog => _api.Catalog;

    public ApiModel Api => _api;

    public AssemblyModel Assembly => ApiCatalogSchema.ApiDeclarationStructure.Assembly.Read(_api.Catalog, _offset);

    public ObsoletionModel? Obsoletion
    {
        get
        {
            return _api.Catalog.GetObsoletion(_api, Assembly);
        }
    }

    public IEnumerable<PlatformSupportModel> PlatformSupport
    {
        get
        {
            return _api.Catalog.GetPlatformSupport(_api, Assembly);
        }
    }

    public PreviewRequirementModel? PreviewRequirement
    {
        get
        {
            return _api.Catalog.GetPreviewRequirement(_api, Assembly);
        }
    }

    public ExperimentalModel? Experimental
    {
        get
        {
            return _api.Catalog.GetExperimental(_api, Assembly);
        }
    }

    public int MarkupId => ApiCatalogSchema.ApiDeclarationStructure.SyntaxOffset.Read(_api.Catalog, _offset);

    public Markup GetMyMarkup()
    {
        var markupOffset = ApiCatalogSchema.ApiDeclarationStructure.SyntaxOffset.Read(_api.Catalog, _offset);
        return _api.Catalog.GetMarkup(markupOffset);
    }

    public Markup GetMarkup()
    {
        var assembly = Assembly;
        var markups = Api.AncestorsAndSelf()
                         .Select(a => a.Declarations.Single(d => d.Assembly == assembly))
                         .Select(d => d.GetMyMarkup())
                         .ToList();
        markups.Reverse();

        var tokens = new List<MarkupToken>();

        var indent = 0;

        foreach (var markup in markups)
        {
            if (indent > 0)
            {
                if (indent - 1 > 0)
                    AddIndentation(tokens, indent - 1);
                tokens.Add(new MarkupToken(MarkupTokenKind.OpenBraceToken));
                tokens.Add(new MarkupToken(MarkupTokenKind.LineBreak));
            }

            var needsIndent = true;

            foreach (var token in markup.Tokens)
            {
                if (needsIndent)
                {
                    // Add indentation
                    AddIndentation(tokens, indent);
                    needsIndent = false;
                }

                tokens.Add(token);

                if (token.Kind == MarkupTokenKind.LineBreak)
                    needsIndent = true;
            }

            tokens.Add(new MarkupToken(MarkupTokenKind.LineBreak));

            indent++;
        }

        for (var i = markups.Count - 1 - 1; i >= 0; i--)
        {
            if (i > 0)
                AddIndentation(tokens, i);
            tokens.Add(new MarkupToken(MarkupTokenKind.CloseBraceToken));
            tokens.Add(new MarkupToken(MarkupTokenKind.LineBreak));
        }

        return new Markup(tokens);

        static void AddIndentation(List<MarkupToken> tokens, int indent)
        {
            for (var i = 0; i < indent * 4; i++)
                tokens.Add(new MarkupToken(MarkupTokenKind.Space));
        }
    }

    public bool IsOverride()
    {
        var api = Api;

        // Finalizers are overrides

        if (api.Kind == ApiKind.Destructor)
            return true;

        var canBeOverriden = api.Kind is ApiKind.Property or
                                         ApiKind.PropertyGetter or
                                         ApiKind.PropertySetter or
                                         ApiKind.Method or
                                         ApiKind.Event or
                                         ApiKind.EventAdder or
                                         ApiKind.EventRemover or
                                         ApiKind.EventRaiser;

        if (canBeOverriden)
        {
            var markup = GetMyMarkup();
            return markup.Tokens.Any(t => t.Kind == MarkupTokenKind.OverrideKeyword);
        }

        return false;
    }

    public PreviewRequirementModel? GetEffectivePreviewRequirement()
    {
        var assembly = Assembly;

        foreach (var api in Api.AncestorsAndSelf())
        {
            if (api.Kind == ApiKind.Namespace)
                break;

            var declaration = api.Declarations.First(d => d.Assembly == assembly);
            if (declaration.PreviewRequirement is not null)
                return declaration.PreviewRequirement;
        }

        return assembly.PreviewRequirement;
    }

    public ExperimentalModel? GetEffectiveExperimental()
    {
        var assembly = Assembly;

        foreach (var ancestor in Api.AncestorsAndSelf())
        {
            if (ancestor.Kind == ApiKind.Namespace)
                break;

            var declaration = ancestor.Declarations.First(d => d.Assembly == assembly);
            if (declaration.Experimental is not null)
                return declaration.Experimental;
        }

        return assembly.Experimental;
    }

    public override bool Equals(object? obj)
    {
        return obj is ApiDeclarationModel model && Equals(model);
    }

    public bool Equals(ApiDeclarationModel other)
    {
        return _api == other._api &&
               _offset == other._offset;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_api, _offset);
    }

    public static bool operator ==(ApiDeclarationModel left, ApiDeclarationModel right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ApiDeclarationModel left, ApiDeclarationModel right)
    {
        return !(left == right);
    }
}