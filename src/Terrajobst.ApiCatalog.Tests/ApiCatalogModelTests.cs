using System.Text;

namespace Terrajobst.ApiCatalog.Tests;

public class ApiCatalogModelTests
{
    [Fact]
    public void Empty_Version_NonZero()
    {
        Assert.True(ApiCatalogModel.Empty.FormatVersion > 0);
    }

    [Fact]
    public void Empty_Statistics_Zero()
    {
        var statistics = ApiCatalogModel.Empty.GetStatistics();

        Assert.Equal(0, statistics.SizeCompressed);
        Assert.Equal(0, statistics.SizeUncompressed);
        Assert.Equal(0, statistics.NumberOfApis);
        Assert.Equal(0, statistics.NumberOfExtensionMethods);
        Assert.Equal(0, statistics.NumberOfDeclarations);
        Assert.Equal(0, statistics.NumberOfAssemblies);
        Assert.Equal(0, statistics.NumberOfFrameworks);
        Assert.Equal(0, statistics.NumberOfFrameworkAssemblies);
        Assert.Equal(0, statistics.NumberOfPackages);
        Assert.Equal(0, statistics.NumberOfPackageVersions);
        Assert.Equal(0, statistics.NumberOfPackageAssemblies);

        foreach (var row in statistics.TableSizes)
        {
            Assert.Equal(0, row.Bytes);

            if (row.TableName is "String Heap" or "Blob Heap")
                Assert.Equal(-1, row.Rows);
            else
                Assert.Equal(0, row.Rows);
        }
    }

    [Fact]
    public void Empty_Collections_Empty()
    {
        var catalog = ApiCatalogModel.Empty;

        Assert.Empty(catalog.Frameworks);
        Assert.Empty(catalog.Platforms);
        Assert.Empty(catalog.Packages);
        Assert.Empty(catalog.Assemblies);
        Assert.Empty(catalog.RootApis);
        Assert.Empty(catalog.AllApis);
        Assert.Empty(catalog.ExtensionMethods);
    }

    [Fact]
    public async Task Read_Empty_Throws()
    {
        await using var stream = new MemoryStream();
        await Assert.ThrowsAsync<InvalidDataException>(() => ApiCatalogModel.LoadAsync(stream));
    }

    [Fact]
    public async Task Read_MagicNumber_Invalid_Throws()
    {
        var invalidMagicNumber = "APIC_TFB"u8.ToArray();
        await using var stream = new MemoryStream(invalidMagicNumber);
        await Assert.ThrowsAsync<InvalidDataException>(() => ApiCatalogModel.LoadAsync(stream));
    }

    [Fact]
    public async Task Read_Version_TooOld_Throws()
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
        {
            writer.Write("APICATFB"u8);
            writer.Write(1);
        }

        stream.Position = 0;

        await Assert.ThrowsAsync<InvalidDataException>(() => ApiCatalogModel.LoadAsync(stream));
    }

    [Fact]
    public async Task Read_Version_TooNew_Throws()
    {
        await using var stream = new MemoryStream();
        await using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
        {
            writer.Write("APICATFB"u8);
            writer.Write(999_999_999);
        }

        stream.Position = 0;

        await Assert.ThrowsAsync<InvalidDataException>(() => ApiCatalogModel.LoadAsync(stream));
    }

    [Fact]
    public async Task GetApiById()
    {
        var source = """
            namespace System
            {
                public class TheClass
                {
                    public TheClass() { }
                }
            }
            """;

        var catalog = await new FluentCatalogBuilder()
            .AddFramework("net461", fx =>
                fx.AddAssembly("System.Runtime", source))
            .BuildAsync();

        var ctor = catalog.AllApis.Single(a => a.Kind == ApiKind.Constructor);
        var result = catalog.GetApiById(ctor.Id);
        Assert.Equal(ctor, result);
    }

    [Fact]
    public async Task GetApiByGuid()
    {
        var source = """
            namespace System
            {
                public class TheClass
                {
                    public TheClass() { }
                }
            }
            """;

        var catalog = await new FluentCatalogBuilder()
            .AddFramework("net461", fx =>
                fx.AddAssembly("System.Runtime", source))
            .BuildAsync();

        var ctor = catalog.AllApis.Single(a => a.Kind == ApiKind.Constructor);
        var result = catalog.GetApiByGuid(ctor.Guid);
        Assert.Equal(ctor, result);
    }

    [Fact]
    public async Task Type()
    {
        var source = """
            namespace System
            {
                public class TheClass
                {
                    private TheClass() { }
                }
            }
            """;

        var catalog = await new FluentCatalogBuilder()
            .AddFramework("net461", fx =>
                fx.AddAssembly("System.Runtime", source))
            .BuildAsync();

        var type = catalog.AllApis.Single(a => a.Name == "TheClass");
        var typeNamespace = type.Parent!.Value;

        Assert.Equal("TheClass", type.Name);
        Assert.Equal(ApiKind.Class, type.Kind);
        Assert.Equal(Guid.Parse("6f0315b1-0b64-5af1-51f6-8f8dec1c4aac"), type.Guid);
        Assert.Equal(catalog, type.Catalog);
        Assert.Empty(type.Children);

        Assert.Equal("System.TheClass", type.GetFullName());
        Assert.Equal("System", type.GetNamespaceName());
        Assert.Equal("TheClass", type.GetTypeName());
        Assert.Equal("", type.GetMemberName());

        Assert.Equal("System", typeNamespace.GetFullName());
        Assert.Equal(ApiKind.Namespace, typeNamespace.Kind);
        Assert.Equal(type, Assert.Single(typeNamespace.Children));

        Assert.Equal(typeNamespace, Assert.Single(catalog.RootApis));
    }

    [Fact]
    public async Task Type_WithoutNamespace()
    {
        var source = """
            public class TheClass
            {
                private TheClass() { }
            }
            """;

        var catalog = await new FluentCatalogBuilder()
            .AddFramework("net461", fx =>
                fx.AddAssembly("System.Runtime", source))
            .BuildAsync();

        var type = catalog.AllApis.Single(a => a.Name == "TheClass");
        var typeNamespace = type.Parent!.Value;

        Assert.Equal("TheClass", type.Name);
        Assert.Equal(ApiKind.Class, type.Kind);
        Assert.Equal(Guid.Parse("8b632220-7064-d2e5-60f8-47053a5bcc06"), type.Guid);
        Assert.Equal(catalog, type.Catalog);
        Assert.Empty(type.Children);

        Assert.Equal("<global namespace>.TheClass", type.GetFullName());
        Assert.Equal("<global namespace>", type.GetNamespaceName());
        Assert.Equal("TheClass", type.GetTypeName());
        Assert.Equal("", type.GetMemberName());

        Assert.Equal("<global namespace>", typeNamespace.GetFullName());
        Assert.Equal(ApiKind.Namespace, typeNamespace.Kind);
        Assert.Equal(type, Assert.Single(typeNamespace.Children));

        Assert.Equal(typeNamespace, Assert.Single(catalog.RootApis));
    }

    [Fact]
    public async Task Type_Declaration_Framework()
    {
        var source = """
            namespace System
            {
                public class TheClass
                {
                    private TheClass() { }
                }
            }
            """;

        var catalog = await new FluentCatalogBuilder()
            .AddFramework("net461", fx =>
                fx.AddAssembly("System.Runtime", source))
            .BuildAsync();

        var type = catalog.AllApis.Single(a => a.Name == "TheClass");

        var declaration = Assert.Single(type.Declarations);
        var assembly = Assert.Single(catalog.Assemblies);
        var framework = Assert.Single(catalog.Frameworks);
        Assert.Empty(catalog.Packages);
        var frameworkAssembly = Assert.Single(framework.Assemblies);

        Assert.Equal("System.Runtime", assembly.Name);
        Assert.Equal("net461", framework.Name);

        Assert.Equal(assembly, declaration.Assembly);
        Assert.Equal(assembly, frameworkAssembly);
    }

    [Fact]
    public async Task Type_Declaration_Package()
    {
        var source = """
            namespace System
            {
                public class TheClass
                {
                    private TheClass() { }
                }
            }
            """;

        var catalog = await new FluentCatalogBuilder()
            .AddPackage("System.Oob", "1.0.0", p => p
                .AddFramework("netstandard2.0", fx =>
                    fx.AddAssembly("System.Runtime", source)))
            .BuildAsync();

        var type = catalog.AllApis.Single(a => a.Name == "TheClass");

        var declaration = Assert.Single(type.Declarations);
        var assembly = Assert.Single(catalog.Assemblies);
        var framework = Assert.Single(catalog.Frameworks);
        var package = Assert.Single(catalog.Packages);
        Assert.Empty(framework.Assemblies);
        var (packagedFramework, packagedAssembly) = Assert.Single(package.Assemblies);

        Assert.Equal("System.Runtime", assembly.Name);
        Assert.Equal("netstandard2.0", framework.Name);

        Assert.Equal(assembly, declaration.Assembly);
        Assert.Equal(assembly, packagedAssembly);

        Assert.Equal(framework, packagedFramework);
    }

    [Fact]
    public async Task Type_Ancestors()
    {
        var source = """
            namespace System
            {
                public class TheClass
                {
                    private TheClass() { }
                }
            }
            """;

        var catalog = await new FluentCatalogBuilder()
            .AddFramework("net461", fx =>
                fx.AddAssembly("System.Runtime", source))
            .BuildAsync();

        var theClass = catalog.AllApis.Single(a => a.Name == "TheClass");
        var system = catalog.AllApis.Single(a => a.Name == "System");

        Assert.Equal(system.AncestorsAndSelf(), new[] { system });
        Assert.Equal(system.Ancestors(), Array.Empty<ApiModel>());

        Assert.Equal(theClass.AncestorsAndSelf(), new[] { theClass, system });
        Assert.Equal(theClass.Ancestors(), new[] { system });
    }

    [Fact]
    public async Task Type_Descendants()
    {
        var source = """
            namespace System
            {
                public class TheClass
                {
                    private TheClass() { }
                }
            }
            """;

        var catalog = await new FluentCatalogBuilder()
            .AddFramework("net461", fx =>
                fx.AddAssembly("System.Runtime", source))
            .BuildAsync();

        var theClass = catalog.AllApis.Single(a => a.Name == "TheClass");
        var system = catalog.AllApis.Single(a => a.Name == "System");

        Assert.Equal(system.DescendantsAndSelf(), new[] { system, theClass });
        Assert.Equal(system.Descendants(), new[] { theClass });

        Assert.Equal(theClass.DescendantsAndSelf(), new[] { theClass });
        Assert.Equal(theClass.Descendants(), Array.Empty<ApiModel>());
    }

    [Fact]
    public async Task Member()
    {
        var source = """
            namespace TheNamespace
            {
                public class TheClass
                {
                    private TheClass() { }
                    public int TheMethod(string p) { return -1; }
                }
            }
            """;

        var catalog = await new FluentCatalogBuilder()
            .AddFramework("net461", fx =>
                fx.AddAssembly("System.Runtime", source))
            .BuildAsync();

        var theClass = catalog.AllApis.Single(a => a.Name == "TheClass");
        var theMethod = catalog.AllApis.Single(a => a.Name == "TheMethod(String)");
        var theNamespace = catalog.AllApis.Single(a => a.Name == "TheNamespace");

        Assert.Equal("TheMethod(String)", theMethod.Name);
        Assert.Equal(ApiKind.Method, theMethod.Kind);
        Assert.Equal(Guid.Parse("709c195e-e2a5-bb55-81ed-72511f3bb783"), theMethod.Guid);
        Assert.Equal(catalog, theMethod.Catalog);
        Assert.Empty(theMethod.Children);

        Assert.Equal(theNamespace, Assert.Single(catalog.RootApis));
        Assert.Equal(theClass, Assert.Single(theNamespace.Children));
        Assert.Equal(theMethod, Assert.Single(theClass.Children));
    }

    [Fact]
    public async Task Type_RequiresPreviewFeature()
    {
        var source = """
            using System.Runtime.Versioning;
            namespace System
            {
                [RequiresPreviewFeatures("This API is in preview")]
                public class TheClass { }
            }
            """;

        var catalog = await new FluentCatalogBuilder()
            .AddFramework("net6.0", fx =>
                fx.AddAssembly("System.Runtime", source))
            .BuildAsync();

        var api = catalog.AllApis.Single(a => a.GetFullName() == "System.TheClass");

        var declaration = Assert.Single(api.Declarations);

        Assert.NotNull(declaration.PreviewRequirement);
        Assert.Equal("This API is in preview", declaration.PreviewRequirement!.Value.Message);
        Assert.Equal("", declaration.PreviewRequirement!.Value.Url);
    }

    [Fact]
    public async Task Type_RequiresPreviewFeature_Url()
    {
        var source = """
            using System.Runtime.Versioning;
            namespace System
            {
                [RequiresPreviewFeatures("This API is in preview", Url="https://aka.ms/test")]
                public class TheClass { }
            }
            """;

        var catalog = await new FluentCatalogBuilder()
            .AddFramework("net6.0", fx =>
                fx.AddAssembly("System.Runtime", source))
            .BuildAsync();

        var api = catalog.AllApis.Single(a => a.GetFullName() == "System.TheClass");

        var declaration = Assert.Single(api.Declarations);

        Assert.NotNull(declaration.PreviewRequirement);
        Assert.Equal("This API is in preview", declaration.PreviewRequirement!.Value.Message);
        Assert.Equal("https://aka.ms/test", declaration.PreviewRequirement!.Value.Url);
    }

    [Fact]
    public async Task Type_Experimental()
    {
        var source = """
            using System;
            using System.Diagnostics.CodeAnalysis;

            namespace System.Diagnostics.CodeAnalysis
            {
                [AttributeUsage(AttributeTargets.Assembly |
                                AttributeTargets.Module |
                                AttributeTargets.Class |
                                AttributeTargets.Struct |
                                AttributeTargets.Enum |
                                AttributeTargets.Constructor |
                                AttributeTargets.Method |
                                AttributeTargets.Property |
                                AttributeTargets.Field |
                                AttributeTargets.Event |
                                AttributeTargets.Interface |
                                AttributeTargets.Delegate, Inherited = false)]
                public sealed class ExperimentalAttribute : Attribute
                {
                    public ExperimentalAttribute(string diagnosticId) { }
                    public string DiagnosticId { get; }
                    public string? UrlFormat { get; set; }
                }
            }

            namespace System
            {
                [Experimental("SYSLIB0042")]
                public class TheClass { }
            }
            """;

        var catalog = await new FluentCatalogBuilder()
            .AddFramework("net6.0", fx =>
                fx.AddAssembly("System.Runtime", source))
            .BuildAsync();

        var api = catalog.AllApis.Single(a => a.GetFullName() == "System.TheClass");

        var declaration = Assert.Single(api.Declarations);

        Assert.NotNull(declaration.Experimental);
        Assert.Equal("SYSLIB0042", declaration.Experimental!.Value.DiagnosticId);
        Assert.Equal("", declaration.Experimental!.Value.Url);
    }

    [Fact]
    public async Task Type_Experimental_Url()
    {
        var source = """
            using System;
            using System.Diagnostics.CodeAnalysis;

            namespace System.Diagnostics.CodeAnalysis
            {
                [AttributeUsage(AttributeTargets.Assembly |
                                AttributeTargets.Module |
                                AttributeTargets.Class |
                                AttributeTargets.Struct |
                                AttributeTargets.Enum |
                                AttributeTargets.Constructor |
                                AttributeTargets.Method |
                                AttributeTargets.Property |
                                AttributeTargets.Field |
                                AttributeTargets.Event |
                                AttributeTargets.Interface |
                                AttributeTargets.Delegate, Inherited = false)]
                public sealed class ExperimentalAttribute : Attribute
                {
                    public ExperimentalAttribute(string diagnosticId) { }
                    public string DiagnosticId { get; }
                    public string? UrlFormat { get; set; }
                }
            }

            namespace System
            {
                [Experimental("SYSLIB0042", UrlFormat="https://aka.ms/syslib/{0}")]
                public class TheClass { }
            }
            """;

        var catalog = await new FluentCatalogBuilder()
            .AddFramework("net6.0", fx =>
                fx.AddAssembly("System.Runtime", source))
            .BuildAsync();

        var api = catalog.AllApis.Single(a => a.GetFullName() == "System.TheClass");

        var declaration = Assert.Single(api.Declarations);

        Assert.NotNull(declaration.Experimental);
        Assert.Equal("SYSLIB0042", declaration.Experimental!.Value.DiagnosticId);
        Assert.Equal("https://aka.ms/syslib/{0}", declaration.Experimental!.Value.UrlFormat);
        Assert.Equal("https://aka.ms/syslib/SYSLIB0042", declaration.Experimental!.Value.Url);
    }

    [Fact]
    public async Task Type_Obsoletion()
    {
        var source = """
            namespace System
            {
                [Obsolete("Do not use")]
                public class TheClass { }
            }
            """;

        var catalog = await new FluentCatalogBuilder()
            .AddFramework("net461", fx =>
                fx.AddAssembly("System.Runtime", source))
            .BuildAsync();

        var api = catalog.AllApis.Single(a => a.GetFullName() == "System.TheClass");

        var declaration = Assert.Single(api.Declarations);

        Assert.NotNull(declaration.Obsoletion);
        Assert.Equal("Do not use", declaration.Obsoletion!.Value.Message);
        Assert.Equal("", declaration.Obsoletion!.Value.Url);
        Assert.Equal("", declaration.Obsoletion!.Value.UrlFormat);
        Assert.Equal("", declaration.Obsoletion!.Value.DiagnosticId);
        Assert.False(declaration.Obsoletion!.Value.IsError);
    }

    [Fact]
    public async Task Type_Obsoletion_IsError()
    {
        var source = """
            namespace System
            {
                [Obsolete("Do not use", true)]
                public class TheClass { }
            }
            """;

        var catalog = await new FluentCatalogBuilder()
            .AddFramework("net461", fx =>
                fx.AddAssembly("System.Runtime", source))
            .BuildAsync();

        var api = catalog.AllApis.Single(a => a.GetFullName() == "System.TheClass");

        var declaration = Assert.Single(api.Declarations);

        Assert.NotNull(declaration.Obsoletion);
        Assert.Equal("Do not use", declaration.Obsoletion!.Value.Message);
        Assert.Equal("", declaration.Obsoletion!.Value.Url);
        Assert.Equal("", declaration.Obsoletion!.Value.UrlFormat);
        Assert.Equal("", declaration.Obsoletion!.Value.DiagnosticId);
        Assert.True(declaration.Obsoletion!.Value.IsError);
    }

    [Fact]
    public async Task Type_Obsoletion_DiagnosticId()
    {
        var source = """
            namespace System
            {
                [Obsolete("Do not use", DiagnosticId = "CA123")]
                public class TheClass { }
            }
            """;

        var catalog = await new FluentCatalogBuilder()
            .AddFramework("net461", fx =>
                fx.AddAssembly("System.Runtime", source))
            .BuildAsync();

        var api = catalog.AllApis.Single(a => a.GetFullName() == "System.TheClass");

        var declaration = Assert.Single(api.Declarations);

        Assert.NotNull(declaration.Obsoletion);
        Assert.Equal("Do not use", declaration.Obsoletion!.Value.Message);
        Assert.Equal("", declaration.Obsoletion!.Value.Url);
        Assert.Equal("", declaration.Obsoletion!.Value.UrlFormat);
        Assert.Equal("CA123", declaration.Obsoletion!.Value.DiagnosticId);
        Assert.False(declaration.Obsoletion!.Value.IsError);
    }

    [Fact]
    public async Task Type_Obsoletion_UrlFormat()
    {
        var source = """
            namespace System
            {
                [Obsolete("Do not use", UrlFormat = "https://aka.ms/an-issue")]
                public class TheClass { }
            }
            """;

        var catalog = await new FluentCatalogBuilder()
            .AddFramework("net461", fx =>
                fx.AddAssembly("System.Runtime", source))
            .BuildAsync();

        var api = catalog.AllApis.Single(a => a.GetFullName() == "System.TheClass");

        var declaration = Assert.Single(api.Declarations);

        Assert.NotNull(declaration.Obsoletion);
        Assert.Equal("Do not use", declaration.Obsoletion!.Value.Message);
        Assert.Equal("https://aka.ms/an-issue", declaration.Obsoletion!.Value.Url);
        Assert.Equal("https://aka.ms/an-issue", declaration.Obsoletion!.Value.UrlFormat);
        Assert.Equal("", declaration.Obsoletion!.Value.DiagnosticId);
        Assert.False(declaration.Obsoletion!.Value.IsError);
    }

    [Fact]
    public async Task Type_Obsoletion_UrlFormat_And_DiagnosticId()
    {
        var source = """
            namespace System
            {
                [Obsolete("Do not use", UrlFormat = "https://aka.ms/{0}", DiagnosticId="CA123")]
                public class TheClass { }
            }
            """;

        var catalog = await new FluentCatalogBuilder()
            .AddFramework("net461", fx =>
                fx.AddAssembly("System.Runtime", source))
            .BuildAsync();

        var api = catalog.AllApis.Single(a => a.GetFullName() == "System.TheClass");

        var declaration = Assert.Single(api.Declarations);

        Assert.NotNull(declaration.Obsoletion);
        Assert.Equal("Do not use", declaration.Obsoletion!.Value.Message);
        Assert.Equal("https://aka.ms/CA123", declaration.Obsoletion!.Value.Url);
        Assert.Equal("https://aka.ms/{0}", declaration.Obsoletion!.Value.UrlFormat);
        Assert.Equal("CA123", declaration.Obsoletion!.Value.DiagnosticId);
        Assert.False(declaration.Obsoletion!.Value.IsError);
    }

    [Fact]
    public async Task Type_PlatformSupport()
    {
        var source = """
            using System.Runtime.Versioning;
            namespace System
            {
                [UnsupportedOSPlatform("windows")]
                [SupportedOSPlatform("windows10")]
                [UnsupportedOSPlatform("browser")]
                public class TheClass { }
            }
            """;

        var catalog = await new FluentCatalogBuilder()
            .AddFramework("net461", fx =>
                fx.AddAssembly("System.Runtime", source))
            .BuildAsync();

        var api = catalog.AllApis.Single(a => a.GetFullName() == "System.TheClass");
        var declaration = Assert.Single(api.Declarations);

        var actual = declaration.PlatformSupport.Select(ps => (ps.PlatformName, ps.IsSupported))
                                                .OrderBy(t => t.PlatformName)
                                                .ToArray();
        var expected = new[] {
            ("browser", false),
            ("windows", false),
            ("windows10", true)
        };

        Assert.Equal(expected, actual);

        var expectedPlatforms = expected.Select(t => t.Item1).Distinct();
        Assert.Equal(expectedPlatforms, catalog.Platforms.OrderBy(p => p.Name).Select(p => p.Name));
    }

    [Fact]
    public async Task Type_Availability_Framework()
    {
        var source = """
            namespace System
            {
                public class TheClass { }
            }
            """;

        var catalog = await new FluentCatalogBuilder()
            .AddFramework("net461", fx =>
                fx.AddAssembly("System.Runtime", source))
            .AddFramework("net462", fx =>
                fx.AddAssembly("System.Runtime", string.Empty))
            .AddPackage("System.Oob", "1.0.0", p =>
                p.AddFramework("netstandard2.0", fx =>
                    fx.AddAssembly("System.Runtime", source)))
            .BuildAsync();

        var api = catalog.AllApis.Single(a => a.GetFullName() == "System.TheClass");
        var availability = api.GetAvailability();

        // .NET Standard 2.0 applies to .NET Framework 4.6.1. However, since the framework includes
        // the API, we expect this to be considered in-box.
        var net461 = availability.Frameworks.Single(fx => fx.Framework.GetShortFolderName() == "net461");
        Assert.True(net461.IsInBox);

        // The API does no exist in .NET Framework 4.6.2 anymore. We expect the API to show up as available
        // via the package.
        var net462 = availability.Frameworks.Single(fx => fx.Framework.GetShortFolderName() == "net462");
        Assert.False(net462.IsInBox);
        Assert.Equal("System.Oob", net462.Package!.Value.Name);
    }

    [Fact]
    public async Task Method_Extension()
    {
        var source = """
            using System.Collections.Generic;

            namespace System
            {
                public class TheClass { }

                public static class TheExtension
                {
                    public static void TheMethod(this TheClass c, int x) { }
                }
            }
            """;

        var catalog = await new FluentCatalogBuilder()
            .AddFramework("net461", fx =>
                fx.AddAssembly("System.Runtime", source))
            .BuildAsync();

        var theClass = catalog.AllApis.Single(a => a.Name == "TheClass");
        var theMethod = catalog.AllApis.Single(a => a.Name == "TheMethod(TheClass, Int32)");

        var extensionMethod = Assert.Single(catalog.ExtensionMethods);
        Assert.Equal(theClass, extensionMethod.ExtendedType);
        Assert.Equal(theMethod, extensionMethod.ExtensionMethod);
        Assert.Equal(extensionMethod, Assert.Single(theClass.ExtensionMethods));

        var extensionMethodByGuid = catalog.GetExtensionMethodByGuid(extensionMethod.Guid);
        Assert.Equal(extensionMethod, extensionMethodByGuid);
    }

    [Fact]
    public async Task Method_Extension_Multiple()
    {
        var source = """
            using System.Collections.Generic;

            namespace System
            {
                public class TheClass1 { }

                public class TheClass2 { }

                public static class TheExtension1
                {
                    public static void M1_1(this TheClass1 c) { }
                    public static void M1_2(this TheClass2 c) { }
                    public static void M1_3(this TheClass1 c) { }
                    public static void M1_4(this TheClass2 c) { }
                }

                public static class TheExtension2
                {
                    public static void M2_1(this TheClass1 c) { }
                    public static void M2_2(this TheClass2 c) { }
                    public static void M2_3(this TheClass1 c) { }
                    public static void M2_4(this TheClass2 c) { }
                }
            }
            """;

        var catalog = await new FluentCatalogBuilder()
            .AddFramework("net461", fx =>
                fx.AddAssembly("System.Runtime", source))
            .BuildAsync();

        var theClass1 = catalog.AllApis.Single(a => a.Name == "TheClass1");
        var theClass2 = catalog.AllApis.Single(a => a.Name == "TheClass2");

        var theClass1Extensions = theClass1.ExtensionMethods.Select(m => m.ExtensionMethod.Name).OrderBy(x => x).ToArray();
        var theClass2Extensions = theClass2.ExtensionMethods.Select(m => m.ExtensionMethod.Name).OrderBy(x => x).ToArray();

        var theClass1ExtensionsExpected = new[] { "M1_1(TheClass1)", "M1_3(TheClass1)", "M2_1(TheClass1)", "M2_3(TheClass1)" };
        var theClass2ExtensionsExpected = new[] { "M1_2(TheClass2)", "M1_4(TheClass2)", "M2_2(TheClass2)", "M2_4(TheClass2)" };

        Assert.Equal(theClass1ExtensionsExpected, theClass1Extensions);
        Assert.Equal(theClass2ExtensionsExpected, theClass2Extensions);
    }

    [Fact]
    public async Task Assembly_RootApis()
    {
        var source = """
            namespace System
            {
                public class TheClass
                {
                    private TheClass() { }
                }
            }
            """;

        var catalog = await new FluentCatalogBuilder()
            .AddFramework("net461", fx =>
                fx.AddAssembly("System.Runtime", source))
            .BuildAsync();

        var assembly = Assert.Single(catalog.Assemblies);
        var type = catalog.AllApis.Single(a => a.Name == "TheClass");
        var typeNamespace = type.Parent;

        Assert.Equal(typeNamespace, Assert.Single(assembly.RootApis));
    }

    [Fact]
    public async Task Assemblies_Unified_Between_Frameworks()
    {
        var source = """
            namespace System
            {
                public class TheClass { }
            }
            """;

        var catalog = await new FluentCatalogBuilder()
            .AddFramework("net461", fx =>
                fx.AddAssembly("System.Runtime", source))
            .AddFramework("net462", fx =>
                fx.AddAssembly("System.Runtime", source))
            .BuildAsync();

        var assembly = Assert.Single(catalog.Assemblies);
        Assert.Equal("System.Runtime", assembly.Name);

        var assemblyFrameworks = assembly.Frameworks.OrderBy(fx => fx.Name).Select(fx => fx.Name).ToArray();
        Assert.Equal(new[] { "net461", "net462" }, assemblyFrameworks);
    }

    [Fact]
    public async Task Assemblies_Unified_Between_Packages()
    {
        var source = """
            namespace System
            {
                public class TheClass { }
            }
            """;

        var catalog = await new FluentCatalogBuilder()
            .AddPackage("System.Oob", "1.0.0", p =>
                p.AddFramework("netstandard2.0", fx =>
                    fx.AddAssembly("System.Runtime", source)))
            .AddPackage("System.Oob.Another", "2.1.0", p =>
                p.AddFramework("netstandard2.1", fx =>
                    fx.AddAssembly("System.Runtime", source)))
            .BuildAsync();

        var assembly = Assert.Single(catalog.Assemblies);
        Assert.Equal("System.Runtime", assembly.Name);

        var assemblyPackages = assembly.Packages.OrderBy(p => p.Package.Name).Select(p => p.Package.Name).ToArray();
        Assert.Equal(new[] { "System.Oob", "System.Oob.Another" }, assemblyPackages);
    }

    [Fact]
    public async Task Assemblies_Unified_Between_Frameworks_and_Packages()
    {
        var source = """
            namespace System
            {
                public class TheClass { }
            }
            """;

        var catalog = await new FluentCatalogBuilder()
            .AddFramework("net45", fx =>
                fx.AddAssembly("System.Runtime", source))
            .AddPackage("System.Oob", "1.0.0", p =>
                p.AddFramework("netstandard2.0", fx =>
                    fx.AddAssembly("System.Runtime", source)))
            .BuildAsync();

        var assembly = Assert.Single(catalog.Assemblies);
        Assert.Equal("System.Runtime", assembly.Name);

        var framework = Assert.Single(assembly.Frameworks);
        Assert.Equal("net45", framework.Name);

        var package = Assert.Single(assembly.Packages);
        Assert.Equal("System.Oob", package.Package.Name);
    }
}