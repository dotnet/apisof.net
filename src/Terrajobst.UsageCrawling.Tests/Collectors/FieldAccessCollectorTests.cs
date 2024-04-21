using Terrajobst.UsageCrawling.Collectors;
using Terrajobst.UsageCrawling.Tests.Infra;

namespace Terrajobst.UsageCrawling.Tests.Collectors;

public class FieldAccessCollectorTests : CollectorTest<FieldAccessCollector>
{
    [Fact]
    public void FieldAccess_DoesNotReport_SelfDefined_Read()
    {
        var source =
            """
            class C {
                int F;

                void M()
                {
                    System.Console.WriteLine(F);
                }
            }
            """;

        Check(source, []);
    }

    [Fact]
    public void FieldAccess_DoesNotReport_SelfDefined_Write()
    {
        var source =
            """
            class C {
                string S;

                void M()
                {
                    S = System.Console.ReadLine();
                }
            }
            """;

        Check(source, []);
    }

    [Fact]
    public void FieldAccess_Report_Static_Read()
    {
        var dependencySource =
            """
            public static class D
            {
                public static int F;
            }
            """;

        var source =
            """
            using System;
            class C
            {
                void M()
                {
                    Console.WriteLine(D.F);
                }
            }
            """;

        Check(dependencySource, source, [FeatureUsage.ForFieldRead("F:D.F")]);
    }

    [Fact]
    public void FieldAccess_Report_Static_Write()
    {
        var dependencySource =
            """
            public static class D
            {
                public static int F;
            }
            """;

        var source =
            """
            class C
            {
                void M()
                {
                    D.F = 42;
                }
            }
            """;

        Check(dependencySource, source, [FeatureUsage.ForFieldWrite("F:D.F")]);
    }

    [Fact]
    public void FieldAccess_Report_Instance_Read()
    {
        var dependencySource =
            """
            public class D
            {
                public int F;
            }
            """;

        var source =
            """
            using System;
            class C
            {
                void M()
                {
                    var d = new D();
                    Console.WriteLine(d.F);
                }
            }
            """;

        Check(dependencySource, source, [FeatureUsage.ForFieldRead("F:D.F")]);
    }

    [Fact]
    public void FieldAccess_Report_Instance_Write()
    {
        var dependencySource =
            """
            public class D
            {
                public int F;
            }
            """;

        var source =
            """
            class C
            {
                void M()
                {
                    var d = new D();
                    d.F = 42;
                }
            }
            """;

        Check(dependencySource, source, [FeatureUsage.ForFieldWrite("F:D.F")]);
    }

    [Fact]
    public void FieldAccess_Report_Generic_Read()
    {
        var source =
            """
            using System;
            class C {
                void M()
                {
                    var t = (1, 2);
                    Console.WriteLine(t.Item2);
                }
            }
            """;

        Check(source, [FeatureUsage.ForFieldRead("F:System.ValueTuple`2.Item2")]);
    }

    [Fact]
    public void FieldAccess_Report_Generic_Write()
    {
        var source =
            """
            class C {
                void M()
                {
                    var t = (1, 2);
                    t.Item2 = 3;
                }
            }
            """;

        Check(source, [FeatureUsage.ForFieldWrite("F:System.ValueTuple`2.Item2")]);
    }

}