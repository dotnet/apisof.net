using Terrajobst.UsageCrawling.Collectors;
using Terrajobst.UsageCrawling.Tests.Infra;

namespace Terrajobst.UsageCrawling.Tests.Collectors;

public class DefinesAnyVirtualStaticInterfaceMembersCollectorTests : CollectorTest<DefinesAnyVirtualStaticInterfaceMembersCollector>
{
    [Fact]
    public void DefinesAnyVirtualStaticInterfaceMembers_DoesNotReport_Regular_Methods()
    {
        var source =
            """
            public interface I {
                void M();
            }
            """;

        Check(source, []);
    }

    [Fact]
    public void DefinesAnyVirtualStaticInterfaceMembers_DoesNotReport_Regular_Properties()
    {
        var source =
            """
            public interface I {
                int P1 { get; }
                int P2 { get; set; }
                int P3 { set; }
            }
            """;

        Check(source, []);
    }
    [Fact]
    public void DefinesAnyVirtualStaticInterfaceMembers_DoesNotReport_NonVirtual_Static_Methods()
    {
        var source =
            """
            public interface I {
                static void M() { }
            }
            """;

        Check(source, []);
    }

    [Fact]
    public void DefinesAnyVirtualStaticInterfaceMembers_DoesNotReport_NonVirtual_Static_Properties()
    {
        var source =
            """
            public interface I {
                static int P => 42;
            }
            """;

        Check(source, []);
    }

    [Fact]
    public void DefinesAnyVirtualStaticInterfaceMembers_Reports_Virtual_Static_Methods()
    {
        var source =
            """
            public interface I {
                virtual static void M() { }
            }
            """;

        Check(source, [UsageMetric.DefinesAnyVirtualStaticInterfaceMembers]);
    }

    [Fact]
    public void DefinesAnyVirtualStaticInterfaceMembers_Reports_Virtual_Static_Properties()
    {
        var source =
            """
            public interface I {
                virtual static int P => 42;
            }
            """;

        Check(source, [UsageMetric.DefinesAnyVirtualStaticInterfaceMembers]);
    }

    [Fact]
    public void DefinesAnyVirtualStaticInterfaceMembers_Reports_Abstract_Static_Methods()
    {
        var source =
            """
            public interface I {
                abstract static void M();
            }
            """;

        Check(source, [UsageMetric.DefinesAnyVirtualStaticInterfaceMembers]);
    }

    [Fact]
    public void DefinesAnyVirtualStaticInterfaceMembers_Reports_Abstract_Static_Properties()
    {
        var source =
            """
            public interface I {
                abstract static int P { get; }
            }
            """;

        Check(source, [UsageMetric.DefinesAnyVirtualStaticInterfaceMembers]);
    }
}