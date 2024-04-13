using Terrajobst.UsageCrawling.Collectors;
using Terrajobst.UsageCrawling.Tests.Infra;

namespace Terrajobst.UsageCrawling.Tests.Collectors;

public class DefinesAnyRefStructsCollectorTests : CollectorTest<DefinesAnyRefStructsCollector>
{
    [Fact]
    public void DefinesAnyRefStructs_DoesNotReport_Structs()
    {
        var source =
            """
            public struct R { }
            """;

        Check(source, []);
    }

    [Fact]
    public void DefinesAnyRefStructs_DoesNotReport_Use()
    {
        var source =
            """
            using System;
            public struct S {
                public void M(Span<byte> s) { }
            }
            """;

        Check(source, []);
    }

    [Fact]
    public void DefinesAnyRefStructs_Reports_PublicRefStructs()
    {
        var source =
            """
            public ref struct R { }
            """;

        Check(source, [UsageMetric.DefinesAnyRefStructs]);
    }

    [Fact]
    public void DefinesAnyRefStructs_Reports_InternalRefStructs()
    {
        var source =
            """
            internal ref struct R { }
            """;

        Check(source, [UsageMetric.DefinesAnyRefStructs]);
    }

    [Fact]
    public void DefinesAnyRefStructs_Reports_NestedRefStructs()
    {
        var source =
            """
            public class C {
                public ref struct R { }
            }
            """;

        Check(source, [UsageMetric.DefinesAnyRefStructs]);
    }
}