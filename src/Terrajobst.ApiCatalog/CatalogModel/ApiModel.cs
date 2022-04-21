using System.Collections;
using System.Text;

namespace Terrajobst.ApiCatalog;

public readonly struct ApiModel : IEquatable<ApiModel>, IComparable<ApiModel>
{
    private readonly ApiCatalogModel _catalog;
    private readonly int _offset;

    internal ApiModel(ApiCatalogModel catalog, int offset)
    {
        _catalog = catalog;
        _offset = offset;
    }

    public ApiCatalogModel Catalog => _catalog;

    public int Id => _offset;

    public Guid Guid
    {
        get
        {
            return _catalog.ApiTable.ReadGuid(_offset);
        }
    }

    public ApiKind Kind
    {
        get
        {
            return (ApiKind)_catalog.ApiTable.ReadByte(_offset + 16);
        }
    }

    public ApiModel? Parent
    {
        get
        {
            var parentOffset = _catalog.ApiTable.ReadInt32(_offset + 17);
            if (parentOffset == -1)
                return null;

            return new ApiModel(_catalog, parentOffset);
        }
    }

    public string Name
    {
        get
        {
            var stringOffset = _catalog.ApiTable.ReadInt32(_offset + 21);
            return _catalog.GetString(stringOffset);
        }
    }

    public ApiCatalogModel.ApiEnumerator Children
    {
        get
        {
            return new ApiCatalogModel.ApiEnumerator(_catalog, _offset + 25);
        }
    }

    public DeclarationEnumerator Declarations
    {
        get
        {
            var childCount = _catalog.ApiTable.ReadInt32(_offset + 25);
            var declarationTableOffset = _offset + 29 + childCount * 4;
            return new DeclarationEnumerator(this, declarationTableOffset);
        }
    }

    public UsageEnumerator Usages
    {
        get
        {
            var childCount = _catalog.ApiTable.ReadInt32(_offset + 25);
            var declarationTableOffset = _offset + 29 + childCount * 4;

            var declarationCount = _catalog.ApiTable.ReadInt32(declarationTableOffset);
            var usagesTableOffset = declarationTableOffset + 4 + declarationCount * 8;
            return new UsageEnumerator(this, usagesTableOffset);
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

    public ApiAvailability GetAvailability()
    {
        return ApiAvailability.Create(this);
    }

    public string GetHelpLink()
    {
        var segments = AncestorsAndSelf().Reverse();

        var sb = new StringBuilder();
        var inAngleBrackets = false;
        var numberOfGenerics = 0;

        foreach (var s in segments)
        {
            if (sb.Length > 0)
                sb.Append('.');

            foreach (var c in s.Name)
            {
                if (inAngleBrackets)
                {
                    if (c == ',')
                    {
                        numberOfGenerics++;
                    }
                    else if (c == '>')
                    {
                        inAngleBrackets = false;

                        if (s.Kind.IsType())
                        {
                            sb.Append('-');
                            sb.Append(numberOfGenerics);
                        }
                    }
                    continue;
                }

                if (c == '(')
                {
                    break;
                }
                else if (c == '<')
                {
                    inAngleBrackets = true;
                    numberOfGenerics = 1;
                    continue;
                }
                else
                {
                    sb.Append(char.ToLower(c));
                }
            }
        }

        var path = sb.ToString();

        return $"https://docs.microsoft.com/en-us/dotnet/api/{path}";
    }

    public override bool Equals(object obj)
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

    public struct DeclarationEnumerator : IEnumerable<ApiDeclarationModel>, IEnumerator<ApiDeclarationModel>
    {
        private readonly ApiModel _apiModel;
        private readonly int _offset;
        private readonly int _count;
        private int _index;

        public DeclarationEnumerator(ApiModel apiModel, int offset)
        {
            _apiModel = apiModel;
            _offset = offset;
            _count = _apiModel.Catalog.ApiTable.ReadInt32(offset);
            _index = -1;
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
            if (_index >= _count - 1)
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

        public ApiDeclarationModel Current
        {
            get
            {
                var offset = _offset + 4 + 8 * _index;
                return new ApiDeclarationModel(_apiModel, offset);
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
        private readonly ApiModel _apiModel;
        private readonly int _offset;
        private readonly int _count;
        private int _index;

        public UsageEnumerator(ApiModel apiModel, int offset)
        {
            _apiModel = apiModel;
            _offset = offset;
            _count = _apiModel.Catalog.ApiTable.ReadInt32(offset);
            _index = -1;
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
            if (_index >= _count - 1)
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

        public ApiUsageModel Current
        {
            get
            {
                var offset = _offset + 4 + 8 * _index;
                return new ApiUsageModel(_apiModel, offset);
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