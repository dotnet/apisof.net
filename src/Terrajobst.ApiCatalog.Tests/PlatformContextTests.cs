namespace Terrajobst.ApiCatalog.Tests;

public class PlatformContextTests
{
    [Fact]
    public async Task PlatformContext_AllowList()
    {
        var source = """
            using System.Runtime.Versioning;
            namespace System
            {
                [SupportedOSPlatform("ios")]
                [SupportedOSPlatform("tvos")]
                public class TheClass { }
            }
            """;

        var expectedPlatforms = """
            The API is only supported on these platforms:
            - iOS
            - Mac Catalyst
            - tvOS
            """;

        var catalog = await new FluentCatalogBuilder()
            .AddFramework("net45", fx =>
                fx.AddAssembly("System.Runtime", GetOperatingSystemWithImpliedPlatform())
                  .AddAssembly("System.TheAssembly", source))
            .BuildAsync();

        var context = PlatformAnnotationContext.Create(catalog, "net45");
        var api = Assert.Single(catalog.AllApis.Where(a => a.GetFullName() == "System.TheClass"));
        var annotation = context.GetPlatformAnnotation(api);

        Assert.True(annotation.IsSupported("ios"));
        Assert.True(annotation.IsSupported("tvos"));
        Assert.True(annotation.IsSupported("maccatalyst"));
        Assert.False(annotation.IsSupported("windows"));

        Assert.Equal(expectedPlatforms, annotation.ToString().Trim());
    }

    [Fact]
    public async Task PlatformContext_AllowList_RemoveImpliedPlatform()
    {
        var source = """
            using System.Runtime.Versioning;
            namespace System
            {
                [SupportedOSPlatform("ios")]
                [SupportedOSPlatform("tvos")]
                [UnsupportedOSPlatform("maccatalyst")]
                public class TheClass { }
            }
            """;

        var expectedPlatforms = """
            The API is only supported on these platforms:
            - iOS
            - tvOS
            """;

        var catalog = await new FluentCatalogBuilder()
            .AddFramework("net45", fx =>
                fx.AddAssembly("System.Runtime", GetOperatingSystemWithImpliedPlatform())
                  .AddAssembly("System.TheAssembly", source))
            .BuildAsync();

        var context = PlatformAnnotationContext.Create(catalog, "net45");
        var api = Assert.Single(catalog.AllApis.Where(a => a.GetFullName() == "System.TheClass"));
        var annotation = context.GetPlatformAnnotation(api);

        Assert.True(annotation.IsSupported("ios"));
        Assert.True(annotation.IsSupported("tvos"));
        Assert.False(annotation.IsSupported("maccatalyst"));
        Assert.False(annotation.IsSupported("windows"));

        Assert.Equal(expectedPlatforms, annotation.ToString().Trim());
    }

    [Fact]
    public async Task PlatformContext_DenyList()
    {
        var source = """
            using System.Runtime.Versioning;
            namespace System
            {
                [UnsupportedOSPlatform("ios")]
                [UnsupportedOSPlatform("tvos")]
                public class TheClass { }
            }
            """;

        var expectedPlatforms = """
            The API is supported on any platform except for:
            - iOS
            - Mac Catalyst
            - tvOS
            """;

        var catalog = await new FluentCatalogBuilder()
            .AddFramework("net45", fx =>
                fx.AddAssembly("System.Runtime", GetOperatingSystemWithImpliedPlatform())
                  .AddAssembly("System.TheAssembly", source))
            .BuildAsync();

        var context = PlatformAnnotationContext.Create(catalog, "net45");
        var api = Assert.Single(catalog.AllApis.Where(a => a.GetFullName() == "System.TheClass"));
        var annotation = context.GetPlatformAnnotation(api);

        Assert.False(annotation.IsSupported("ios"));
        Assert.False(annotation.IsSupported("tvos"));
        Assert.False(annotation.IsSupported("maccatalyst"));
        Assert.True(annotation.IsSupported("windows"));

        Assert.Equal(expectedPlatforms, annotation.ToString().Trim());
    }

    [Fact]
    public async Task PlatformContext_DenyList_AddImpliedPlatform()
    {
        var source = """
            using System.Runtime.Versioning;
            namespace System
            {
                [SupportedOSPlatform("maccatalyst")]
                [UnsupportedOSPlatform("ios")]
                [UnsupportedOSPlatform("tvos")]
                public class TheClass { }
            }
            """;

        var expectedPlatforms = """
            The API is supported on any platform except for:
            - iOS
            - tvOS
            """;

        var catalog = await new FluentCatalogBuilder()
            .AddFramework("net45", fx =>
                fx.AddAssembly("System.Runtime", GetOperatingSystemWithImpliedPlatform())
                  .AddAssembly("System.TheAssembly", source))
            .BuildAsync();

        var context = PlatformAnnotationContext.Create(catalog, "net45");
        var api = Assert.Single(catalog.AllApis.Where(a => a.GetFullName() == "System.TheClass"));
        var annotation = context.GetPlatformAnnotation(api);

        Assert.False(annotation.IsSupported("ios"));
        Assert.False(annotation.IsSupported("tvos"));
        Assert.True(annotation.IsSupported("maccatalyst"));
        Assert.True(annotation.IsSupported("windows"));

        Assert.Equal(expectedPlatforms, annotation.ToString().Trim());
    }

    [Fact]
    public async Task PlatformContext_Assembly()
    {
        var source = """
            using System.Runtime.Versioning;
            [assembly: UnsupportedOSPlatform("ios")]
            namespace System
            {
                public class TheClass { }
            }
            """;

        var expectedPlatforms = """
            The API is supported on any platform except for:
            - iOS
            """;

        var catalog = await new FluentCatalogBuilder()
            .AddFramework("net45", fx =>
                fx.AddAssembly("System.TheAssembly", source))
            .BuildAsync();

        var context = PlatformAnnotationContext.Create(catalog, "net45");
        var api = Assert.Single(catalog.AllApis.Where(a => a.GetFullName() == "System.TheClass"));
        var annotation = context.GetPlatformAnnotation(api);

        Assert.False(annotation.IsSupported("ios"));
        Assert.True(annotation.IsSupported("windows"));

        Assert.Equal(expectedPlatforms, annotation.ToString().Trim());
    }

    [Fact]
    public async Task PlatformContext_Type()
    {
        var source = """
            using System.Runtime.Versioning;
            namespace System
            {
                [UnsupportedOSPlatform("ios")]
                public class TheClass
                {
                    public void TheMethod() {}
                }
            }
            """;

        var expectedPlatforms = """
            The API is supported on any platform except for:
            - iOS
            """;

        var catalog = await new FluentCatalogBuilder()
            .AddFramework("net45", fx =>
                fx.AddAssembly("System.TheAssembly", source))
            .BuildAsync();

        var context = PlatformAnnotationContext.Create(catalog, "net45");
        var api = Assert.Single(catalog.AllApis.Where(a => a.GetFullName() == "System.TheClass.TheMethod()"));
        var annotation = context.GetPlatformAnnotation(api);

        Assert.False(annotation.IsSupported("ios"));
        Assert.True(annotation.IsSupported("windows"));

        Assert.Equal(expectedPlatforms, annotation.ToString().Trim());
    }

    [Fact]
    public async Task PlatformContext_Property()
    {
        var source = """
            using System.Runtime.Versioning;
            namespace System
            {
                public class TheClass
                {
                    [SupportedOSPlatform("ios")]
                    public int TheProperty => 0;
                }
            }
            """;

        var expectedPlatforms = """
            The API is only supported on these platforms:
            - iOS
            """;

        var catalog = await new FluentCatalogBuilder()
            .AddFramework("net45", fx =>
                fx.AddAssembly("System.TheAssembly", source))
            .BuildAsync();

        var context = PlatformAnnotationContext.Create(catalog, "net45");
        var api = Assert.Single(catalog.AllApis.Where(a => a.GetFullName() == "System.TheClass.TheProperty.TheProperty.get"));
        var annotation = context.GetPlatformAnnotation(api);

        Assert.True(annotation.IsSupported("ios"));
        Assert.False(annotation.IsSupported("windows"));

        Assert.Equal(expectedPlatforms, annotation.ToString().Trim());
    }

    [Fact]
    public async Task PlatformContext_Property_Accessor()
    {
        var source = """
            using System.Runtime.Versioning;
            namespace System
            {
                public class TheClass
                {
                    public int TheProperty
                    {
                        [UnsupportedOSPlatform("ios")]
                        get => 0;
                    }
                }
            }
            """;

        var expectedPlatforms = """
            The API is supported on any platform except for:
            - iOS
            """;

        var catalog = await new FluentCatalogBuilder()
            .AddFramework("net45", fx =>
                fx.AddAssembly("System.TheAssembly", source))
            .BuildAsync();

        var context = PlatformAnnotationContext.Create(catalog, "net45");
        var api = Assert.Single(catalog.AllApis.Where(a => a.GetFullName() == "System.TheClass.TheProperty.TheProperty.get"));
        var annotation = context.GetPlatformAnnotation(api);

        Assert.False(annotation.IsSupported("ios"));
        Assert.True(annotation.IsSupported("windows"));

        Assert.Equal(expectedPlatforms, annotation.ToString().Trim());
    }

    [Fact]
    public async Task PlatformContext_Event()
    {
        var source = """
            using System;
            using System.Runtime.Versioning;
            namespace System
            {
                public class TheClass
                {
                    [SupportedOSPlatform("ios")]
                    public event EventHandler TheEvent;
                }
            }
            """;

        var expectedPlatforms = """
            The API is only supported on these platforms:
            - iOS
            """;

        var catalog = await new FluentCatalogBuilder()
            .AddFramework("net45", fx =>
                fx.AddAssembly("System.TheAssembly", source))
            .BuildAsync();

        var context = PlatformAnnotationContext.Create(catalog, "net45");
        var api = Assert.Single(catalog.AllApis.Where(a => a.GetFullName() == "System.TheClass.TheEvent.TheEvent.add"));
        var annotation = context.GetPlatformAnnotation(api);

        Assert.True(annotation.IsSupported("ios"));
        Assert.False(annotation.IsSupported("windows"));

        Assert.Equal(expectedPlatforms, annotation.ToString().Trim());
    }

    [Fact]
    public async Task PlatformContext_Event_Accessor()
    {
        var source = """
            using System;
            using System.Runtime.Versioning;
            namespace System
            {
                public class TheClass
                {
                    public event EventHandler TheEvent
                    {
                        [UnsupportedOSPlatform("ios")]
                        add { }
                        remove { }
                    }
                }
            }
            """;

        var expectedPlatforms = """
            The API is supported on any platform except for:
            - iOS
            """;

        var catalog = await new FluentCatalogBuilder()
            .AddFramework("net45", fx =>
                fx.AddAssembly("System.TheAssembly", source))
            .BuildAsync();

        var context = PlatformAnnotationContext.Create(catalog, "net45");
        var api = Assert.Single(catalog.AllApis.Where(a => a.GetFullName() == "System.TheClass.TheEvent.TheEvent.add"));
        var annotation = context.GetPlatformAnnotation(api);

        Assert.False(annotation.IsSupported("ios"));
        Assert.True(annotation.IsSupported("windows"));

        Assert.Equal(expectedPlatforms, annotation.ToString().Trim());
    }

    private static string GetOperatingSystemWithImpliedPlatform()
    {
        return """
            using System.Runtime.Versioning;

            namespace System
            {
                public sealed class OperatingSystem
                {
                    [SupportedOSPlatformGuard("maccatalyst")]
                    public static bool IsIOS() { return false; }
                }
            }

            namespace System.Runtime.Versioning
            {
                public sealed class SupportedOSPlatformGuardAttribute : OSPlatformAttribute
                {
                    public SupportedOSPlatformGuardAttribute(string platformName) : base(platformName) { }
                }

                public abstract class OSPlatformAttribute : Attribute
                {
                    protected OSPlatformAttribute(string platformName) { }
                }
            }
            """;
    }
}