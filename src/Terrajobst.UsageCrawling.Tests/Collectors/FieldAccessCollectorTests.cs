using Terrajobst.UsageCrawling.Collectors;
using Terrajobst.UsageCrawling.Tests.Infra;

namespace Terrajobst.UsageCrawling.Tests.Collectors;

public class FieldAccessCollectorTests : CollectorTest<FieldAccessCollector>
{
    [Fact]
    public void FieldAccess_DoesNotReport_NoUsage()
    {
        var source =
            """
            class C {
                int X;
            }
            """;

        Check(source, []);
    }

    [Fact]
    public void FieldAccess_Report_Static_Read()
    {
        var source =
            """
            using System;
            class C {
                static int X;

                void M()
                {
                    Console.WriteLine(X);
                }
            }
            """;

        Check(source, [FeatureUsage.ForFieldRead("F:C.X")]);
    }

    [Fact]
    public void FieldAccess_Report_Static_Write()
    {
        var source =
            """
            class C {
                static int X;

                void M()
                {
                    X = 42;
                }
            }
            """;

        Check(source, [FeatureUsage.ForFieldWrite("F:C.X")]);
    }

    [Fact]
    public void FieldAccess_Report_Instance_Read()
    {
        var source =
            """
            using System;
            class C {
                int X;

                void M()
                {
                    Console.WriteLine(X);
                }
            }
            """;

        Check(source, [FeatureUsage.ForFieldRead("F:C.X")]);
    }

    [Fact]
    public void FieldAccess_Report_Instance_Write()
    {
        var source =
            """
            class C {
                int X;

                void M()
                {
                    X = 42;
                }
            }
            """;

        Check(source, [FeatureUsage.ForFieldWrite("F:C.X")]);
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
                    var x = t.Item2;
                    Console.WriteLine(x);
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