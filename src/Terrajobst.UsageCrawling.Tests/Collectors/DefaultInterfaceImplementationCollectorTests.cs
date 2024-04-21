using Terrajobst.UsageCrawling.Collectors;
using Terrajobst.UsageCrawling.Tests.Infra;

namespace Terrajobst.UsageCrawling.Tests.Collectors;

public class DefaultInterfaceImplementationCollectorTests : CollectorTest<DefaultInterfaceImplementationCollector>
{
    [Fact]
    public void DefaultInterfaceImplementation_DoesNotReport_SelfDefined()
    {
        var source =
            """
            public interface IMine
            {
                int Count { get; }
            }

            public interface I<T> : IMine
            {
                int IMine.Count => 42;
            }
            """;

        base.Check(source, []);
    }

    [Fact]
    public void DefaultInterfaceImplementation_DoesNotReport_SelfDefined_Generic()
    {
        var source =
            """
            public interface IMine<T>
            {
                int Count { get; }
            }

            public interface I<T> : IMine<T>
            {
                int IMine<T>.Count => 42;
            }
            """;

        base.Check(source, []);
    }

    [Fact]
    public void DefaultInterfaceImplementation_DoesNotReport_New()
    {
        var source =
            """
            using System.Collections.Generic;

            public interface I<T> : IReadOnlyCollection<T>
            {
                new int Count => 42;
            }
            """;

        base.Check(source, []);
    }

    [Fact]
    public void DefaultInterfaceImplementation_Reports_Dim()
    {
        var source =
            """
            using System.Collections.Generic;

            public interface I<T> : IReadOnlyCollection<T>
            {
                int IReadOnlyCollection<T>.Count => 42;
            }
            """;

        var expected =
            """
            M:System.Collections.Generic.IReadOnlyCollection`1.get_Count
            """;

        Check(source, expected);
    }

    private void Check(string source, string expectedIds)
    {
        Check(source, expectedIds, FeatureUsage.ForDim);
    }
}