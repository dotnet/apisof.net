using Terrajobst.UsageCrawling.Collectors;
using Terrajobst.UsageCrawling.Tests.Infra;

namespace Terrajobst.UsageCrawling.Tests.Collectors;

public class DefinesAnyRefFieldsCollectorTests : CollectorTest<DefinesAnyRefFieldsCollector>
{
    [Fact]
    public void DefinesAnyRefFieldsCollector_DoesNotReport_RegularFields()
    {
        var source =
            """
            public struct S
            {
                private int _f1;
                private string _f2;
            }
            """;

        Check(source, []);
    }

    [Fact]
    public void DefinesAnyRefFieldsCollector_DoesNotReport_Spans()
    {
        var source =
            """
            using System;
            public ref struct S
            {
                private Span<int> _f;
            }
            """;

        Check(source, []);
    }

    [Fact]
    public void DefinesAnyRefFieldsCollector_DoesNotReport_Volatile()
    {
        var source =
            """
            public class C
            {
                private volatile int _f1;
                private volatile string _f2;
            }
            """;

        Check(source, []);
    }

    [Fact]
    public void DefinesAnyRefFieldsCollector_Reports_RefFields()
    {
        var source =
            """
            public ref struct C
            {
                private ref int _f;
            }
            """;

        Check(source, [FeatureUsage.DefinesAnyRefFields]);
    }

    [Fact]
    public void DefinesAnyRefFieldsCollector_Reports_RefFields_WithArray()
    {
        var source =
            """
            public ref struct S
            {
                private ref int[] _f;
            }
            """;

        Check(source, [FeatureUsage.DefinesAnyRefFields]);
    }

    [Fact]
    public void DefinesAnyRefFieldsCollector_Reports_RefFields_WithGeneric()
    {
        var source =
            """
            public ref struct S<T>
            {
                private ref T _f;
            }
            """;

        Check(source, [FeatureUsage.DefinesAnyRefFields]);
    }
}