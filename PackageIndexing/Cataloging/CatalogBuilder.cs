using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace PackageIndexing
{
    public abstract class CatalogBuilder
    {
        public void Index(string indexPath)
        {
            var files = Directory.GetFiles(indexPath, "*.xml");
            var fileSet = new PathFileSet(files);

            static void DefineApis(CatalogBuilder builder, IEnumerable<XElement> apiElements)
            {
                foreach (var element in apiElements)
                {
                    var fingerprint = Guid.Parse(element.Attribute("fingerprint").Value);
                    var kind = (ApiKind)int.Parse(element.Attribute("kind").Value);
                    var parentFingerprint = element.Attribute("parent") == null
                        ? Guid.Empty
                        : Guid.Parse(element.Attribute("parent").Value);
                    var name = element.Attribute("name").Value;

                    builder.DefineApi(fingerprint, kind, parentFingerprint, name);
                }
            }

            foreach (var (path, data) in fileSet.GetFiles())
            {
                Console.WriteLine($"Processing {path}...");
                var doc = XDocument.Load(data);
                if (doc.Root.Name == "package")
                {
                    var packageFingerprint = Guid.Parse(doc.Root.Attribute("fingerprint").Value);
                    var packageId = doc.Root.Attribute("id").Value;
                    var packageName = doc.Root.Attribute("name").Value;
                    DefinePackage(packageFingerprint, packageId, packageName);

                    DefineApis(this, doc.Root.Elements("api"));

                    foreach (var assemblyElement in doc.Root.Elements("assembly"))
                    {
                        var framework = assemblyElement.Attribute("fx").Value;
                        var assemblyFingerprint = Guid.Parse(assemblyElement.Attribute("fingerprint").Value);
                        var name = assemblyElement.Attribute("name").Value;
                        var version = assemblyElement.Attribute("version").Value;
                        var publicKeyToken = assemblyElement.Attribute("publicKeyToken").Value;
                        DefineFramework(framework);
                        DefineAssembly(assemblyFingerprint, name, version, publicKeyToken);
                        DefinePackageAssembly(packageFingerprint, framework, assemblyFingerprint);

                        foreach (var syntaxElement in assemblyElement.Elements("syntax"))
                        {
                            var apiFingerprint = Guid.Parse(syntaxElement.Attribute("id").Value);
                            var syntax = syntaxElement.Value;
                            DefineDeclaration(assemblyFingerprint, apiFingerprint, syntax);
                        }
                    }
                }
                else if (doc.Root.Name == "framework")
                {
                    var framework = doc.Root.Attribute("name").Value;
                    DefineFramework(framework);

                    DefineApis((CatalogBuilder)this, doc.Root.Elements("api"));

                    foreach (var assemblyElement in doc.Root.Elements("assembly"))
                    {
                        var assemblyFingerprint = Guid.Parse(assemblyElement.Attribute("fingerprint").Value);
                        var name = assemblyElement.Attribute("name").Value;
                        var version = assemblyElement.Attribute("version").Value;
                        var publicKeyToken = assemblyElement.Attribute("publicKeyToken").Value;
                        DefineAssembly(assemblyFingerprint, name, version, publicKeyToken);
                        DefineFrameworkAssembly(framework, assemblyFingerprint);

                        foreach (var syntaxElement in assemblyElement.Elements("syntax"))
                        {
                            var apiFingerprint = Guid.Parse(syntaxElement.Attribute("id").Value);
                            var syntax = syntaxElement.Value;
                            DefineDeclaration(assemblyFingerprint, apiFingerprint, syntax);
                        }
                    }
                }
            }
        }

        protected abstract void DefineApi(Guid fingerprint, ApiKind kind, Guid parentFingerprint, string name);
        protected abstract void DefineAssembly(Guid fingerprint, string name, string version, string publicKeyToken);
        protected abstract void DefineDeclaration(Guid assemblyFingerprint, Guid apiFingerprint, string syntax);
        protected abstract void DefineFramework(string frameworkName);
        protected abstract void DefineFrameworkAssembly(string framework, Guid assemblyFingerprint);
        protected abstract void DefinePackage(Guid fingerprint, string id, string version);
        protected abstract void DefinePackageAssembly(Guid packageFingerprint, string framework, Guid assemblyFingerprint);
    }
}
