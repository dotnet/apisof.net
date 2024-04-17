using Terrajobst.UsageCrawling.Collectors;
using Terrajobst.UsageCrawling.Tests.Infra;

namespace Terrajobst.UsageCrawling.Tests.Collectors;

public class ExceptionCollectorTests : CollectorTest<ExceptionCollector>
{
    [Fact]
    public void ExceptionCollector_DoesNotReport_EmptyCatch()
    {
        var source =
            """
            class C
            {
                void M()
                {
                    try
                    {
                    }
                    catch
                    {
                    }
                }
            }
            """;

        Check(source, []);
    }

    [Fact]
    public void ExceptionCollector_DoesNotReport_UsageOfException()
    {
        var source =
            """
            using System;
            class MyException : Exception
            {
            }
            """;

        Check(source, []);
    }

    [Fact]
    public void ExceptionCollector_DoesNotReport_NullThrow()
    {
        var source =
            """
            class C
            {
                void M()
                {
                    throw null;
                }
            }
            """;

        Check(source, []);
    }

    [Fact]
    public void ExceptionCollector_DoesReport_Throw()
    {
        var source =
            """
            class C
            {
                void M()
                {
                    throw new System.InvalidOperationException();
                }
            }
            """;

        Check(source, [FeatureUsage.ForExceptionThrow("M:System.InvalidOperationException.#ctor")]);
    }

    [Fact]
    public void ExceptionCollector_DoesReport_Throw_Generic()
    {
        var source =
            """
            using System;
            class MyEx<T> : Exception
            {
                public MyEx(T value) { }
            }
            class C
            {
                void M()
                {
                    throw new MyEx<int>(42);
                }
            }
            """;

        Check(source, [FeatureUsage.ForExceptionThrow("M:MyEx`1.#ctor(`0)")]);
    }

    [Fact]
    public void ExceptionCollector_DoesReport_Catch()
    {
        var source =
            """
            using System;
            using System.IO;
            class C
            {
                void M()
                {
                    try
                    {
                        Console.WriteLine("Test");
                    }
                    catch (IOException ex)
                    {
                        Console.WriteLine(ex);
                    }
                }
            }
            """;

        Check(source, [FeatureUsage.ForExceptionCatch("T:System.IO.IOException")]);
    }

    [Fact]
    public void ExceptionCollector_DoesReport_Catch_Generic()
    {
        var source =
            """
            using System;
            using System.IO;
            class MyEx<T> : Exception
            {
                public MyEx(T value) { }
            }
            class C
            {
                void M()
                {
                    try
                    {
                        Console.WriteLine("Test");
                    }
                    catch (MyEx<int> ex)
                    {
                        Console.WriteLine(ex);
                    }
                }
            }
            """;

        Check(source, [FeatureUsage.ForExceptionCatch("T:MyEx`1")]);
    }

}