using System;
using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ApiCatalog.Metadata;

namespace ApiCatalog.CatalogModel;

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
            var rowSpan = _catalog.ApiTable.Slice(_offset);
            var guidSpan = rowSpan.Slice(0, 16);
            return new Guid(guidSpan);
        }
    }

    public ApiKind Kind
    {
        get
        {
            var rowSpan = _catalog.ApiTable[_offset..];
            return (ApiKind)rowSpan[16..][0];
        }
    }

    public ApiModel Parent
    {
        get
        {
            var rowSpan = _catalog.ApiTable.Slice(_offset);
            var parentOffsetSpan = rowSpan.Slice(17);
            var parentOffset = BinaryPrimitives.ReadInt32LittleEndian(parentOffsetSpan);
            if (parentOffset == -1)
                return default;

            return new ApiModel(_catalog, parentOffset);
        }
    }

    public string Name
    {
        get
        {
            var rowSpan = _catalog.ApiTable.Slice(_offset);
            var stringOffsetSpan = rowSpan.Slice(21);
            var stringOffset = BinaryPrimitives.ReadInt32LittleEndian(stringOffsetSpan);
            return _catalog.GetString(stringOffset);
        }
    }

    public IEnumerable<ApiModel> Children => _catalog.GetApis(_offset + 25);

    public IEnumerable<ApiDeclarationModel> Declarations
    {
        get
        {
            var childCount = _catalog.GetApiTableInt32(_offset + 25);
            var declarationTableOffset = _offset + 29 + childCount * 4;
            var count = _catalog.GetApiTableInt32(declarationTableOffset);

            for (var i = 0; i < count; i++)
            {
                var offset = declarationTableOffset + 4 + i * 8;
                yield return new ApiDeclarationModel(this, offset);
            }
        }
    }

    public IEnumerable<ApiUsageModel> Usages
    {
        get
        {
            var childCount = _catalog.GetApiTableInt32(_offset + 25);
            var declarationTableOffset = _offset + 29 + childCount * 4;

            var declarationCount = _catalog.GetApiTableInt32(declarationTableOffset);
            var usagesTableOffset = declarationTableOffset + 4 + declarationCount * 8;
            var count = _catalog.GetApiTableInt32(usagesTableOffset);

            for (var i = 0; i < count; i++)
            {
                var offset = usagesTableOffset + 4 + i * 8;
                yield return new ApiUsageModel(_catalog, offset);
            }
        }
    }

    public IEnumerable<ApiModel> AncestorsAndSelf()
    {
        var current = this;
        while (current != default)
        {
            yield return current;
            current = current.Parent;
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
        var c = this;
        while (c != default)
        {
            if (sb.Length > 0)
                sb.Insert(0, '.');

            sb.Insert(0, c.Name);
            c = c.Parent;
        }

        return sb.ToString();
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
}