using System;
using System.Collections.Generic;
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
    }
}
