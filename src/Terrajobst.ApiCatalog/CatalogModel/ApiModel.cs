using System.Collections;
using System.Text;
using NuGet.Frameworks;

namespace Terrajobst.ApiCatalog;

public readonly struct ApiModel : IEquatable<ApiModel>, IComparable<ApiModel>
{
    private readonly ApiCatalogModel _catalog;
    private readonly int _offset;

    internal ApiModel(ApiCatalogModel catalog, int offset)
    {
        ThrowIfNull(catalog);
        ThrowIfRowIndexOutOfRange(offset, catalog.ApiTable, ApiCatalogSchema.ApiRow.Size);

        _catalog = catalog;
        _offset = offset;
    }

    public ApiCatalogModel Catalog => _catalog;

    public int Id => _offset;

    public Guid Guid => ApiCatalogSchema.ApiRow.Guid.Read(_catalog, _offset);

    public ApiKind Kind => ApiCatalogSchema.ApiRow.Kind.Read(_catalog, _offset);

    public ApiModel? Parent => ApiCatalogSchema.ApiRow.Parent.Read(_catalog, _offset);

    public string Name => ApiCatalogSchema.ApiRow.Name.Read(_catalog, _offset);

    public ChildrenEnumerator Children
    {
        get
        {
            var enumerator = ApiCatalogSchema.ApiRow.Children.Read(_catalog, _offset);
            return new ChildrenEnumerator(enumerator);
        }
    }

    public DeclarationEnumerator Declarations
    {
        get
        {
            var enumerator = ApiCatalogSchema.ApiRow.Declarations.Read(_catalog, _offset);
            return new DeclarationEnumerator(_offset, enumerator);
        }
    }

    public UsageEnumerator Usages
    {
        get
        {
            var enumerator = ApiCatalogSchema.ApiRow.Usages.Read(_catalog, _offset);
            return new UsageEnumerator(_offset, enumerator);
        }
    }

    public ExtensionMethodEnumerator ExtensionMethods
    {
        get
        {
            return new ExtensionMethodEnumerator(this);
        }
    }

    public IEnumerable<ApiModel> AncestorsAndSelf()
    {
        var current = this;

        while (true)
        {
            yield return current;

            if (current.Parent is null)
                break;

            current = current.Parent.Value;
        }
    }

    public IEnumerable<ApiModel> Ancestors()
    {
        return AncestorsAndSelf().Skip(1);
    }

    public IEnumerable<ApiModel> DescendantsAndSelf()
    {
        var stack = new Stack<ApiModel>();
        stack.Push(this);

        while (stack.Count > 0)
        {
            var current = stack.Pop();

            yield return current;

            foreach (var child in current.Children.Reverse())
                stack.Push(child);
        }
    }

    public IEnumerable<ApiModel> Descendants()
    {
        return DescendantsAndSelf().Skip(1);
    }

    public string GetFullName()
    {
        var sb = new StringBuilder();

        foreach (var c in AncestorsAndSelf())
        {
            if (sb.Length > 0)
                sb.Insert(0, '.');

            sb.Insert(0, c.Name);
        }

        return sb.ToString();
    }

    public ApiModel? GetContainingNamespace()
    {
        return Ancestors().SkipWhile(a => a.Kind != ApiKind.Namespace)
                          .Select(a => (ApiModel?)a)
                          .FirstOrDefault();
    }

    public ApiModel? GetContainingType()
    {
        return Ancestors().SkipWhile(a => !a.Kind.IsType())
                          .Select(a => (ApiModel?)a)
                          .FirstOrDefault();
    }

    public string GetNamespaceName()
    {
        if (Kind == ApiKind.Namespace)
            return GetFullName();

        var containingNamespace = GetContainingNamespace();
        return containingNamespace is not null
            ? containingNamespace.Value.GetFullName()
            : string.Empty;
    }

    public string GetTypeName()
    {
        var containingTypes = AncestorsAndSelf().SkipWhile(a => !a.Kind.IsType())
                                                .TakeWhile(a => a.Kind.IsType());

        var sb = new StringBuilder();
        foreach (var containingType in containingTypes)
        {
            if (sb.Length > 0)
                sb.Insert(0, '.');

            sb.Insert(0, containingType.Name);
        }

        return sb.ToString();
    }

    public string GetMemberName()
    {
        return Kind.IsMember()
                ? Name
                : string.Empty;
    }

    public bool IsAvailable(NuGetFramework framework)
    {
        ThrowIfNull(framework);

        return _catalog.AvailabilityContext.IsAvailable(this, framework);
    }

    public ApiDeclarationModel? GetDefinition(NuGetFramework framework)
    {
        ThrowIfNull(framework);

        return _catalog.AvailabilityContext.GetDefinition(this, framework);
    }

    public ApiAvailability GetAvailability()
    {
        return _catalog.AvailabilityContext.GetAvailability(this);
    }

    public ApiFrameworkAvailability? GetAvailability(NuGetFramework framework)
    {
        ThrowIfNull(framework);

        return _catalog.AvailabilityContext.GetAvailability(this, framework);
    }

    public override bool Equals(object? obj)
    {
        return obj is ApiModel model && Equals(model);
    }

    public bool Equals(ApiModel other)
    {
        return ReferenceEquals(_catalog, other._catalog) &&
               _offset == other._offset;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_catalog, _offset);
    }

    public static bool operator ==(ApiModel left, ApiModel right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ApiModel left, ApiModel right)
    {
        return !(left == right);
    }

    public override string ToString()
    {
        return GetFullName();
    }

    public int CompareTo(ApiModel other)
    {
        if (other == default)
            return 1;

        if (Kind.IsType() && other.Kind.IsMember())
            return -1;

        if (Kind.IsMember() && other.Kind.IsType())
            return 1;

        if (Kind.IsMember() && other.Kind.IsMember())
        {
            var result = Kind.CompareTo(other.Kind);
            if (result != 0)
                return result;
        }

        if (Kind == ApiKind.Namespace && other.Kind == ApiKind.Namespace)
        {
            var orderReversed = new[]
            {
                "Windows",
                "Microsoft",
                "System",
            };

            var topLevel = GetTopLevelNamespace(Name);
            var otherTopLevel = GetTopLevelNamespace(other.Name);

            var topLevelIndex = Array.IndexOf(orderReversed, topLevel);
            var otherTopLevelIndex = Array.IndexOf(orderReversed, otherTopLevel);

            var result = -topLevelIndex.CompareTo(otherTopLevelIndex);
            if (result != 0)
                return result;
        }

        if (GetMemberName(Name) == GetMemberName(other.Name))
        {
            var typeParameterCount = GetTypeParameterCount(Name);
            var otherTypeParameterCount = GetTypeParameterCount(other.Name);

            var result = typeParameterCount.CompareTo(otherTypeParameterCount);
            if (result != 0)
                return result;

            var parameterCount = GetParameterCount(Name);
            var otherParameterCount = GetParameterCount(other.Name);

            result = parameterCount.CompareTo(otherParameterCount);
            if (result != 0)
                return result;
        }

        return Name.CompareTo(other.Name);

        static int GetTypeParameterCount(string name)
        {
            return GetArity(name, '<', '>');
        }

        static int GetParameterCount(string name)
        {
            return GetArity(name, '(', ')');
        }

        static string GetMemberName(string name)
        {
            var angleIndex = name.IndexOf('<');
            var parenthesisIndex = name.IndexOf('(');
            if (angleIndex < 0 && parenthesisIndex < 0)
                return name;

            if (angleIndex >= 0 && parenthesisIndex >= 0)
                return name.Substring(0, Math.Min(angleIndex, parenthesisIndex));

            if (angleIndex >= 0)
                return name.Substring(0, angleIndex);

            return name.Substring(0, parenthesisIndex);
        }

        static int GetArity(string name, char openParenthesis, char closeParenthesis)
        {
            var openIndex = name.IndexOf(openParenthesis);
            if (openIndex < 0)
                return 0;

            var closeIndex = name.IndexOf(closeParenthesis);
            if (closeIndex < 0)
                return 0;

            var result = 1;

            for (var i = openIndex + 1; i < closeIndex; i++)
                if (name[i] == ',')
                    result++;

            return result;
        }

        static string GetTopLevelNamespace(string name)
        {
            var dotIndex = name.IndexOf('.');
            if (dotIndex < 0)
                return name;

            return name.Substring(0, dotIndex);
        }
    }

    public struct ChildrenEnumerator : IEnumerable<ApiModel>, IEnumerator<ApiModel>
    {
        private ApiCatalogSchema.ArrayEnumerator<ApiModel> _enumerator;

        internal ChildrenEnumerator(ApiCatalogSchema.ArrayEnumerator<ApiModel> enumerator)
        {
            _enumerator = enumerator;
        }

        IEnumerator<ApiModel> IEnumerable<ApiModel>.GetEnumerator()
        {
            return GetEnumerator();
        }

        public ChildrenEnumerator GetEnumerator()
        {
            return this;
        }

        public bool MoveNext()
        {
            return _enumerator.MoveNext();
        }

        void IEnumerator.Reset()
        {
            throw new NotSupportedException();
        }

        object IEnumerator.Current
        {
            get { return Current; }
        }

        public ApiModel Current
        {
            get { return _enumerator.Current; }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        void IDisposable.Dispose()
        {
        }
    }

    public struct DeclarationEnumerator : IEnumerable<ApiDeclarationModel>, IEnumerator<ApiDeclarationModel>
    {
        private readonly int _apiOffset;
        private ApiCatalogSchema.ArrayOfStructuresEnumerator<ApiCatalogSchema.ApiDeclarationLayout> _enumerator;

        internal DeclarationEnumerator(int apiOffset, ApiCatalogSchema.ArrayOfStructuresEnumerator<ApiCatalogSchema.ApiDeclarationLayout> enumerator)
        {
            _apiOffset = apiOffset;
            _enumerator = enumerator;
        }

        IEnumerator<ApiDeclarationModel> IEnumerable<ApiDeclarationModel>.GetEnumerator()
        {
            return GetEnumerator();
        }

        public DeclarationEnumerator GetEnumerator()
        {
            return this;
        }

        public bool MoveNext()
        {
            return _enumerator.MoveNext();
        }

        void IEnumerator.Reset()
        {
            throw new NotSupportedException();
        }

        object IEnumerator.Current
        {
            get { return Current; }
        }

        public ApiDeclarationModel Current
        {
            get
            {
                var declarationOffset = _enumerator.Current;
                var api = new ApiModel(_enumerator.Catalog, _apiOffset);
                return new ApiDeclarationModel(api, declarationOffset);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        void IDisposable.Dispose()
        {
        }
    }

    public struct UsageEnumerator : IEnumerable<ApiUsageModel>, IEnumerator<ApiUsageModel>
    {
        private readonly int _apiOffset;
        private ApiCatalogSchema.ArrayOfStructuresEnumerator<ApiCatalogSchema.ApiUsageLayout> _enumerator;

        internal UsageEnumerator(int apiOffset, ApiCatalogSchema.ArrayOfStructuresEnumerator<ApiCatalogSchema.ApiUsageLayout> enumerator)
        {
            _apiOffset = apiOffset;
            _enumerator = enumerator;
        }

        IEnumerator<ApiUsageModel> IEnumerable<ApiUsageModel>.GetEnumerator()
        {
            return GetEnumerator();
        }

        public UsageEnumerator GetEnumerator()
        {
            return this;
        }

        public bool MoveNext()
        {
            return _enumerator.MoveNext();
        }

        void IEnumerator.Reset()
        {
            throw new NotSupportedException();
        }

        object IEnumerator.Current
        {
            get { return Current; }
        }

        public ApiUsageModel Current
        {
            get
            {
                var usageOffset = _enumerator.Current;
                var api = new ApiModel(_enumerator.Catalog, _apiOffset);
                return new ApiUsageModel(api, usageOffset);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        void IDisposable.Dispose()
        {
        }
    }

    public struct ExtensionMethodEnumerator : IEnumerable<ExtensionMethodModel>, IEnumerator<ExtensionMethodModel>
    {
        private readonly ApiModel _apiModel;
        private readonly int _offset;
        private int _index;

        public ExtensionMethodEnumerator(ApiModel apiModel)
        {
            _apiModel = apiModel;
            _offset = _apiModel.Catalog.GetExtensionMethodOffset(_apiModel.Id);
            _index = -1;
        }

        IEnumerator<ExtensionMethodModel> IEnumerable<ExtensionMethodModel>.GetEnumerator()
        {
            return GetEnumerator();
        }

        public ExtensionMethodEnumerator GetEnumerator()
        {
            return this;
        }

        public bool MoveNext()
        {
            var nextRowOffset = _offset + (_index + 1) * ApiCatalogSchema.ExtensionMethodRow.Size;
            if (_offset < 0 || nextRowOffset >= _apiModel.Catalog.ExtensionMethodTable.Length)
                return false;

            var extendedType = ApiCatalogSchema.ExtensionMethodRow.ExtendedType.Read(_apiModel.Catalog, nextRowOffset);
            if (extendedType != _apiModel)
                return false;

            _index++;
            return true;
        }

        void IEnumerator.Reset()
        {
            throw new NotSupportedException();
        }

        object IEnumerator.Current
        {
            get { return Current; }
        }

        public ExtensionMethodModel Current
        {
            get
            {
                var offset = _offset + _index * ApiCatalogSchema.ExtensionMethodRow.Size;
                return new ExtensionMethodModel(_apiModel.Catalog, offset);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        void IDisposable.Dispose()
        {
        }
    }
}