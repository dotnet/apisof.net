using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

using Microsoft.CodeAnalysis;

namespace PackageIndexing
{
    public sealed class AssemblyEntry
    {
        private AssemblyEntry(AssemblyIdentity identity, List<ApiEntry> apis)
        {
            Fingerprint = ComputeFingerprint(identity, apis);
            Identity = identity;
            Apis = apis;
        }

        public Guid Fingerprint { get; }
        public AssemblyIdentity Identity { get; }
        public List<ApiEntry> Apis { get; }

        public static AssemblyEntry Create(IAssemblySymbol assembly)
        {
            var identity = assembly.Identity;
            var apis = GetApis(assembly);
            return new AssemblyEntry(identity, apis);
        }

        private static List<ApiEntry> GetApis(IAssemblySymbol symbol)
        {
            var result = new List<ApiEntry>();
            var types = symbol.GetAllTypes()
                              .Where(t => t.IsIncludedInCatalog())
                              .GroupBy(t => t.ContainingNamespace);

            foreach (var namespaceGroup in types)
            {
                var entry = ApiEntry.Create(namespaceGroup.Key);
                result.Add(entry);

                foreach (var type in namespaceGroup)
                    AddApi(entry, type);
            }

            return result;
        }

        private static void AddApi(ApiEntry parent, ITypeSymbol symbol)
        {
            if (!symbol.IsIncludedInCatalog())
                return;

            var apiEntry = ApiEntry.Create(symbol, parent);
            parent.Children.Add(apiEntry);

            foreach (var member in symbol.GetMembers())
                AddMember(apiEntry, member);
        }

        private static void AddMember(ApiEntry parent, ISymbol symbol)
        {
            if (symbol is INamedTypeSymbol type)
            {
                AddApi(parent, type);
                return;
            }

            if (!symbol.IsIncludedInCatalog())
                return;

            var entry = ApiEntry.Create(symbol, parent);
            parent.Children.Add(entry);
        }

        private static Guid ComputeFingerprint(AssemblyIdentity identity, List<ApiEntry> roots)
        {
            using var stream = new MemoryStream();
            using var writer = new StreamWriter(stream);

            writer.WriteLine(identity.ToString());
            WriteDeclarations(writer, roots);

            writer.Flush();

            stream.Position = 0;

            var md5 = MD5.Create();
            var hashBytes = md5.ComputeHash(stream);
            return new Guid(hashBytes);

            static void WriteDeclarations(StreamWriter writer, List<ApiEntry> roots)
            {
                foreach (var a in roots)
                {
                    writer.WriteLine(a.Syntax);
                    WriteDeclarations(writer, a.Children);
                }
            }
        }

        public IEnumerable<ApiEntry> AllApis()
        {
            var stack = new Stack<ApiEntry>();
            foreach (var api in Apis.AsEnumerable().Reverse())
                stack.Push(api);

            while (stack.Count > 0)
            {
                var api = stack.Pop();
                yield return api;

                foreach (var child in api.Children.AsEnumerable().Reverse())
                    stack.Push(child);
            }
        }
    }
}
