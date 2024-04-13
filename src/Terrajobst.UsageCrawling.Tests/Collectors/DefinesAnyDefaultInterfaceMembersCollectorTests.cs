using Terrajobst.UsageCrawling.Collectors;
using Terrajobst.UsageCrawling.Tests.Infra;

namespace Terrajobst.UsageCrawling.Tests.Collectors;

public class DefinesAnyDefaultInterfaceMembersCollectorTests : CollectorTest<DefinesAnyDefaultInterfaceMembersCollector>
{
    [Fact]
    public void DefinesAnyDefaultInterfaceMembers_DoesNotReport_Regular_Methods()
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
    public void DefinesAnyDefaultInterfaceMembers_DoesNotReport_Regular_Properties()
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
    public void DefinesAnyDefaultInterfaceMembers_DoesNotReport_Abstract_Methods()
    {
        var source =
            """
            public interface I {
                public abstract void M();
            }
            """;

        Check(source, []);
    }

    [Fact]
    public void DefinesAnyDefaultInterfaceMembers_DoesNotReport_Abstract_Properties()
    {
        var source =
            """
            public interface I {
                public abstract int P1 { get; }
                public abstract int P2 { get; set; }
                public abstract int P3 { set; }
            }
            """;

        Check(source, []);
    }

    [Fact]
    public void DefinesAnyDefaultInterfaceMembers_DoesNotReport_Class_Methods()
    {
        var source =
            """
            public abstract class C {
                public virtual void M() { }
            }
            """;

        Check(source, []);
    }

    [Fact]
    public void DefinesAnyDefaultInterfaceMembers_Reports_Default_Methods()
    {
        var source =
            """
            public interface I {
                virtual void M() { }
            }
            """;

        Check(source, [UsageMetric.DefinesAnyDefaultInterfaceMembers]);
    }

    [Fact]
    public void DefinesAnyDefaultInterfaceMembers_Reports_Default_Properties()
    {
        var source =
            """
            public interface I {
                virtual int P => 42;
            }
            """;

        Check(source, [UsageMetric.DefinesAnyDefaultInterfaceMembers]);
    }
}