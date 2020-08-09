using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

using Microsoft.CodeAnalysis;

namespace PackageIndexing
{
    internal static class CatalogExtensions
    {
        private static readonly SymbolDisplayFormat _nameFormat = new SymbolDisplayFormat(
            memberOptions: SymbolDisplayMemberOptions.IncludeParameters,
            parameterOptions: SymbolDisplayParameterOptions.IncludeType,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters            
        );

        public static Guid GetCatalogGuid(this ISymbol symbol)
        {
            if (symbol is ITypeParameterSymbol)
                return Guid.Empty;

            if (symbol is INamespaceSymbol ns && ns.IsGlobalNamespace)
                return GetCatalogGuid("N:<global>");

            var id = symbol.OriginalDefinition.GetDocumentationCommentId();
            if (id == null)
                return Guid.Empty;

            return GetCatalogGuid(id);
        }

        public static Guid GetCatalogGuid(string packageId, string packageVersion)
        {
            return GetCatalogGuid($"{packageId}/{packageVersion}");
        }

        private static Guid GetCatalogGuid(string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            using var md5 = MD5.Create();
            var hashBytes = md5.ComputeHash(bytes);
            return new Guid(hashBytes);
        }

        public static string GetCatalogSyntax(this ISymbol symbol)
        {
            var writer = new StringSyntaxWriter();
            CSharpDeclarationWriter.WriteDeclaration(symbol, writer);
            return writer.ToString();
        }

        public static string GetCatalogSyntaxMarkup(this ISymbol symbol)
        {
            var writer = new MarkupSyntaxWriter();
            CSharpDeclarationWriter.WriteDeclaration(symbol, writer);
            return writer.ToString();
        }

        public static string GetCatalogName(this ISymbol symbol)
        {
            if (symbol is INamespaceSymbol)
                return symbol.ToString();

            if (symbol is INamedTypeSymbol type && type.IsTupleType)
            {
                var sb = new StringBuilder();
                sb.Append(type.Name);

                if (type.TypeParameters.Length > 0)
                {
                    sb.Append("<");

                    for (int i = 0; i < type.TypeParameters.Length; i++)
                    {
                        if (i > 0)
                            sb.Append(", ");

                        sb.Append(type.TypeParameters[i].Name);
                    }

                    sb.Append(">");
                }

                return sb.ToString();
            }

            return symbol.ToDisplayString(_nameFormat);
        }

        public static bool IsAccessor(this ISymbol symbol)
        {
            if (symbol is IMethodSymbol method)
            {
                return method.MethodKind == MethodKind.PropertyGet ||
                       method.MethodKind == MethodKind.PropertySet ||
                       method.MethodKind == MethodKind.EventAdd ||
                       method.MethodKind == MethodKind.EventRemove ||
                       method.MethodKind == MethodKind.EventRaise;
            }

            return false;
        }

        public static ApiKind GetApiKind(this ISymbol symbol)
        {
            if (symbol is INamespaceSymbol)
                return ApiKind.Namespace;

            if (symbol is INamedTypeSymbol type)
            {
                if (type.TypeKind == TypeKind.Interface)
                    return ApiKind.Interface;
                else if (type.TypeKind == TypeKind.Delegate)
                    return ApiKind.Delegate;
                else if (type.TypeKind == TypeKind.Enum)
                    return ApiKind.Enum;
                else if (type.TypeKind == TypeKind.Struct)
                    return ApiKind.Struct;
                else
                    return ApiKind.Class;
            }

            if (symbol is IMethodSymbol method)
            {
                if (method.MethodKind == MethodKind.Constructor)
                    return ApiKind.Constructor;
                else if (method.MethodKind == MethodKind.Destructor)
                    return ApiKind.Destructor;
                else if (method.MethodKind == MethodKind.UserDefinedOperator || method.MethodKind == MethodKind.Conversion)
                    return ApiKind.Operator;

                return ApiKind.Method;
            }

            if (symbol is IFieldSymbol)
                return ApiKind.Field;

            if (symbol is IPropertySymbol)
                return ApiKind.Property;

            if (symbol is IEventSymbol)
                return ApiKind.Event;

            throw new Exception($"Unpexected symbol kind {symbol.Kind}");
        }

        public static string GetPublicKeyTokenString(this AssemblyIdentity identity)
        {
            return BitConverter.ToString(identity.PublicKeyToken.ToArray()).Replace("-", "").ToLower();
        }

        public static IEnumerable<INamedTypeSymbol> GetAllTypes(this IAssemblySymbol symbol)
        {
            var stack = new Stack<INamespaceSymbol>();
            stack.Push(symbol.GlobalNamespace);

            while (stack.Count > 0)
            {
                var ns = stack.Pop();
                foreach (var member in ns.GetMembers())
                {
                    if (member is INamespaceSymbol childNs)
                        stack.Push(childNs);
                    else if (member is INamedTypeSymbol type)
                        yield return type;
                }
            }
        }

        public static bool IsIncludedInCatalog(this ISymbol symbol)
        {
            if (symbol.DeclaredAccessibility != Accessibility.Public &&
                symbol.DeclaredAccessibility != Accessibility.Protected)
                return false;

            if (symbol.ContainingType?.TypeKind == TypeKind.Delegate)
                return false;

            if (symbol.IsAccessor())
                return false;

            return true;
        }

        public static bool IsIncludedInCatalog(this AttributeData attribute)
        {
            if (!attribute.AttributeClass.IsIncludedInCatalog())
                return false;

            if (attribute.AttributeClass.Name == "CompilerGeneratedAttribute")
                return false;

            if (attribute.AttributeClass.Name == "TargetedPatchingOptOutAttribute")
                return false;

            return true;
        }

        public static ImmutableArray<AttributeData> GetCatalogAttributes(this IMethodSymbol method)
        {
            if (method == null)
                return ImmutableArray<AttributeData>.Empty;

            return method.GetAttributes().Where(IsIncludedInCatalog).ToImmutableArray();
        }

        public static IEnumerable<ITypeSymbol> Ordered(this IEnumerable<ITypeSymbol> types)
        {
            var comparer = Comparer<ITypeSymbol>.Create((x, y) =>
            {
                if (x.Name == y.Name)
                {
                    var xGenericArity = 0;
                    var yGenericArity = 0;

                    if (x is INamedTypeSymbol xNamed)
                        xGenericArity = xNamed.TypeParameters.Length;

                    if (y is INamedTypeSymbol yNamed)
                        yGenericArity = yNamed.TypeParameters.Length;

                    var result = xGenericArity.CompareTo(yGenericArity);
                    if (result != 0)
                        return result;
                }

                return x.ToDisplayString().CompareTo(y.ToDisplayString());
            });

            return types.OrderBy(t => t, comparer);
        }

        public static IEnumerable<AttributeData> Ordered(this IEnumerable<AttributeData> attributes)
        {
            return attributes.OrderBy(a => a.AttributeClass.Name)
                             .ThenBy(a => a.ConstructorArguments.Length)
                             .ThenBy(a => a.NamedArguments.Length);
        }

        public static IEnumerable<KeyValuePair<string, TypedConstant>> Ordered(this IEnumerable<KeyValuePair<string, TypedConstant>> namedArguments)
        {
            return namedArguments.OrderBy(kv => kv.Key);
        }
    }
}
