using Terrajobst.UsageCrawling.Collectors;
using Terrajobst.UsageCrawling.Tests.Infra;

namespace Terrajobst.UsageCrawling.Tests.Collectors;

public class DerivesFromCollectorTests : CollectorTest<DerivesFromCollector>
{
    [Fact]
    public void DerivesFromCollector_Reports_Delegates()
    {
        var source =
            """
            delegate void D();
            """;

        Check(source, [FeatureUsage.ForDerivesFrom("T:System.MulticastDelegate")]);
    }

    [Fact]
    public void DerivesFromCollector_Reports_Enum()
    {
        var source =
            """
            enum E {}
            """;

        Check(source, [FeatureUsage.ForDerivesFrom("T:System.Enum")]);
    }

    [Fact]
    public void DerivesFromCollector_Reports_Classes()
    {
        var source =
            """
            using System.Collections;
            class C : ArrayList {}
            """;

        Check(source, [FeatureUsage.ForDerivesFrom("T:System.Collections.ArrayList")]);
    }

    [Fact]
    public void DerivesFromCollector_Reports_Classes_WithGenericTypes()
    {
        var source =
            """
            using System.Collections.Generic;
            class C<T> : List<T> {}
            """;

        Check(source, [FeatureUsage.ForDerivesFrom("T:System.Collections.Generic.List`1")]);
    }

    [Fact]
    public void DerivesFromCollector_Reports_Classes_WithIntantiatedGenericTypes()
    {
        var source =
            """
            using System.Collections.Generic;
            class C : List<int> {}
            """;

        Check(source, [FeatureUsage.ForDerivesFrom("T:System.Collections.Generic.List`1")]);
    }

    [Fact]
    public void DerivesFromCollector_Reports_Classes_LocallyDefined()
    {
        var source =
            """
            using System.Collections;
            class B {}
            class D : B {}
            """;

        Check(source, [
            FeatureUsage.ForDerivesFrom("T:System.Object"),
            FeatureUsage.ForDerivesFrom("T:B")
        ]);
    }

    [Fact]
    public void DerivesFromCollector_Reports_Classes_ImplicitlyDerivingFromObject()
    {
        var source =
            """
            class C {}
            """;

        Check(source, [FeatureUsage.ForDerivesFrom("T:System.Object")]);
    }

    [Fact]
    public void DerivesFromCollector_Reports_Classes_WithInterfaces()
    {
        var source =
            """
            using System.Collections;
            abstract class C : IEnumerable {
                public abstract IEnumerator GetEnumerator();
            }
            """;

        Check(source, [
            FeatureUsage.ForDerivesFrom("T:System.Object"),
            FeatureUsage.ForDerivesFrom("T:System.Collections.IEnumerable")
        ]);
    }

    [Fact]
    public void DerivesFromCollector_Reports_Structs()
    {
        var source =
            """
            struct S {}
            """;

        Check(source, [FeatureUsage.ForDerivesFrom("T:System.ValueType")]);
    }

    [Fact]
    public void DerivesFromCollector_Reports_Interfaces()
    {
        var source =
            """
            using System.Collections;
            interface I : IEnumerable {}
            """;

        Check(source, [FeatureUsage.ForDerivesFrom("T:System.Collections.IEnumerable")]);
    }

    [Fact]
    public void DerivesFromCollector_Reports_Structs_WithInterfaces()
    {
        var source =
            """
            using System.Collections;
            struct S : IEnumerable {
                public IEnumerator GetEnumerator() => throw null;
            }
            """;

        Check(source, [
            FeatureUsage.ForDerivesFrom("T:System.ValueType"),
            FeatureUsage.ForDerivesFrom("T:System.Collections.IEnumerable")
        ]);
    }

    [Fact]
    public void DerivesFromCollector_DoesNotReport_EmptyInterfaces()
    {
        var source =
            """
            interface I {}
            """;

        Check(source, []);
    }
}