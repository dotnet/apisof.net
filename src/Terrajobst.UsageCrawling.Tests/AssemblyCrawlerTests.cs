using System.Text;
using Microsoft.Cci;
using Terrajobst.UsageCrawling.Tests.Infra;

namespace Terrajobst.UsageCrawling.Tests;

public class AssemblyCrawlerTests
{
    [Fact]
    public void Attributes_Assembly()
    {
        const string source =
            """
            using System.Reflection;
            [assembly: AssemblyMetadata("key", "value")]
            """;

        var usages = new Dictionary<string, int>
        {
            { "T:System.Reflection.AssemblyMetadataAttribute", 1 },
            { "M:System.Reflection.AssemblyMetadataAttribute.#ctor(System.String,System.String)", 1 }
        };

        Check(source, usages);
    }

    [Fact]
    public void Attributes_Module()
    {
        const string source =
            """
            using System;
            [module: CLSCompliant(false)]
            """;

        var usages = new Dictionary<string, int>
        {
            { "T:System.CLSCompliantAttribute", 1 },
            { "M:System.CLSCompliantAttribute.#ctor(System.Boolean)", 1 }
        };

        Check(source, usages);
    }

    [Fact]
    public void Attributes_Type()
    {
        const string source =
            """
            using System;
            [Obsolete(DiagnosticId = "x")]
            class Test { }
            """;

        var usages = new Dictionary<string, int>
        {
            { "T:System.Object", 1 },       // Base
            { "M:System.Object.#ctor", 1 }, // Base call
            { "T:System.Void", 1 },         // Constructor
            { "T:System.ObsoleteAttribute", 1 },
            { "M:System.ObsoleteAttribute.#ctor", 1 },
            { "F:System.ObsoleteAttribute.DiagnosticId", 1 },
            { "P:System.ObsoleteAttribute.DiagnosticId", 1 }
        };

        Check(source, usages);
    }

    [Fact]
    public void Attributes_Constructor()
    {
        const string source =
            """
            using System;
            class Test {
                [Obsolete]
                Test() { }
            }
            """;

        var usages = new Dictionary<string, int>
        {
            { "T:System.Object", 1 },       // Base
            { "M:System.Object.#ctor", 1 }, // Base call
            { "T:System.Void", 1 },         // Constructor
            { "T:System.ObsoleteAttribute", 1 },
            { "M:System.ObsoleteAttribute.#ctor", 1 }
        };

        Check(source, usages);
    }

    [Fact]
    public void Attributes_Method()
    {
        const string source =
            """
            using System;
            class Test {
                [Obsolete]
                void DoStuff() { }
            }
            """;

        var usages = new Dictionary<string, int>
        {
            { "T:System.Object", 1 },       // Base
            { "M:System.Object.#ctor", 1 }, // Base call
            { "T:System.Void", 2 },         // Constructor + method
            { "T:System.ObsoleteAttribute", 1 },
            { "M:System.ObsoleteAttribute.#ctor", 1 }
        };

        Check(source, usages);
    }

    [Fact]
    public void Attributes_Field()
    {
        const string source =
            """
            using System;
            class Test {
                [Obsolete]
                int _member;
            }
            """;

        var usages = new Dictionary<string, int>
        {
            { "T:System.Int32", 1 },
            { "T:System.Object", 1 },       // Base
            { "M:System.Object.#ctor", 1 }, // Base call
            { "T:System.Void", 1 },         // Constructor
            { "T:System.ObsoleteAttribute", 1 },
            { "M:System.ObsoleteAttribute.#ctor", 1 }
        };

        Check(source, usages);
    }

    [Fact]
    public void Attributes_Property()
    {
        const string source =
            """
            using System;
            class Test {
                [Obsolete]
                int P { get; set; }
            }
            """;

        var usages = new Dictionary<string, int>
        {
            { "T:System.Int32", 4 },        // Property + getter + setter + field
            { "T:System.Object", 1 },       // Base
            { "M:System.Object.#ctor", 1 }, // Base call
            { "T:System.Void", 2 },         // Constructor + setter
            { "T:System.ObsoleteAttribute", 1 },
            { "M:System.ObsoleteAttribute.#ctor", 1 }
        };

        Check(source, usages);
    }

    [Fact]
    public void Attributes_Event()
    {
        const string source =
            """
            using System;
            class Test {
                [Obsolete]
                event EventHandler E;
            }
            """;

        var usages = new Dictionary<string, int>
        {
            { "T:System.EventHandler", 6 }, // Event + adder + remover + field + 2 casts in generated handlers
            { "T:System.Object", 1 },       // Base
            { "M:System.Object.#ctor", 1 }, // Base call
            { "T:System.Void", 3 },         // Constructor + adder + remover
            { "M:System.Delegate.Combine(System.Delegate,System.Delegate)", 1 }, // Adder
            { "M:System.Delegate.Remove(System.Delegate,System.Delegate)", 1 },  // Remover
            { "T:System.ObsoleteAttribute", 1 },
            { "M:System.ObsoleteAttribute.#ctor", 1 }
        };

        Check(source, usages);
    }

    [Fact]
    public void Type_BaseType()
    {
        const string source =
            """
            class Test { }
            """;

        var usages = new Dictionary<string, int>
        {
            { "T:System.Object", 1 },       // Base
            { "M:System.Object.#ctor", 1 }, // Base call
            { "T:System.Void", 1 },         // Constructor
        };

        Check(source, usages);
    }

    [Fact]
    public void Type_InterfaceImplementation()
    {
        const string source =
            """
            using System.Collections;
            class Test : IEnumerable {
                public IEnumerator GetEnumerator() => null;
            }
            """;

        var usages = new Dictionary<string, int>
        {
            { "T:System.Object", 1 },       // Base
            { "M:System.Object.#ctor", 1 }, // Base call
            { "T:System.Collections.IEnumerable", 1 },
            { "T:System.Collections.IEnumerator", 1 },
            { "T:System.Void", 1 }          // Constructor
        };

        Check(source, usages);
    }

    [Fact]
    public void Method()
    {
        const string source =
            """
            using System;
            class Test {
                public void M() {
                    int.Parse("x");
                }
            }
            """;

        var usages = new Dictionary<string, int>
        {
            { "T:System.Object", 1 },       // Base
            { "M:System.Object.#ctor", 1 }, // Base call
            { "T:System.Void", 2 },         // Constructor + method
            { "M:System.Int32.Parse(System.String)", 1 }
        };

        Check(source, usages);
    }

    [Fact]
    public void Property_Get()
    {
        const string source =
            """
            using System;
            class Test {
                public void M() {
                    var x = "x".Length;
                }
            }
            """;

        var usages = new Dictionary<string, int>
        {
            { "T:System.Object", 1 },       // Base
            { "M:System.Object.#ctor", 1 }, // Base call
            { "T:System.Void", 2 },         // Constructor + method
            { "M:System.String.get_Length", 1 }
        };

        Check(source, usages);
    }

    [Fact]
    public void Property_Set()
    {
        const string source =
            """
            using System;
            class Test {
                public void M() {
                    new ObsoleteAttribute {
                        DiagnosticId = "x"
                    };
                }
            }
            """;

        var usages = new Dictionary<string, int>
        {
            { "T:System.Object", 1 },       // Base
            { "M:System.Object.#ctor", 1 }, // Base call
            { "T:System.Void", 2 },         // Constructor + method
            { "M:System.ObsoleteAttribute.#ctor", 1 },
            { "M:System.ObsoleteAttribute.set_DiagnosticId(System.String)", 1 }
        };

        Check(source, usages);
    }

    [Fact]
    public void Event_Add()
    {
        const string source =
            """
            using System;
            static class Test {
                static void M() {
                    AppDomain.CurrentDomain.UnhandledException += Handler;
                }
                static void Handler(object args, UnhandledExceptionEventArgs e) {}
            }
            """;

        var usages = new Dictionary<string, int>
        {
            { "T:System.Object", 4 }, // Base, event handler sender
            { "T:System.Void", 2 },
            { "M:System.AppDomain.get_CurrentDomain", 1 },
            { "T:System.UnhandledExceptionEventHandler", 2 },
            { "M:System.UnhandledExceptionEventHandler.#ctor(System.Object,System.IntPtr)", 1 },
            { "M:System.AppDomain.add_UnhandledException(System.UnhandledExceptionEventHandler)", 1 },
            { "T:System.UnhandledExceptionEventArgs", 1 }
        };

        Check(source, usages);
    }

    [Fact]
    public void Event_Remove()
    {
        const string source =
            """
            using System;
            static class Test {
                static void M() {
                    AppDomain.CurrentDomain.UnhandledException -= Handler;
                }
                static void Handler(object args, UnhandledExceptionEventArgs e) {}
            }
            """;

        var usages = new Dictionary<string, int>
        {
            { "T:System.Object", 4 }, // Base, event handler sender
            { "T:System.Void", 2 },
            { "M:System.AppDomain.get_CurrentDomain", 1 },
            { "T:System.UnhandledExceptionEventHandler", 2 },
            { "M:System.UnhandledExceptionEventHandler.#ctor(System.Object,System.IntPtr)", 1 },
            { "M:System.AppDomain.remove_UnhandledException(System.UnhandledExceptionEventHandler)", 1 },
            { "T:System.UnhandledExceptionEventArgs", 1 }
        };

        Check(source, usages);
    }

    [Fact]
    public void Field_Read()
    {
        const string source =
            """
            using System.IO;
            static class Test {
                static void M() {
                    var x = Path.PathSeparator;
                }
            }
            """;

        var usages = new Dictionary<string, int>
        {
            { "T:System.Object", 1 }, // Base
            { "T:System.Void", 1 },
            { "F:System.IO.Path.PathSeparator", 1 }
        };

        Check(source, usages);
    }

    [Fact]
    public void Field_Write()
    {
        const string source =
            """
            using System.Runtime.InteropServices;
            static class Test {
                static void M() {
                    new MarshalAsAttribute((short)0) { MarshalType = "" };
                }
            }
            """;

        var usages = new Dictionary<string, int>
        {
            { "T:System.Object", 1 }, // Base
            { "T:System.Void", 1 },
            { "M:System.Runtime.InteropServices.MarshalAsAttribute.#ctor(System.Int16)", 1 },
            { "F:System.Runtime.InteropServices.MarshalAsAttribute.MarshalType", 1 }
        };

        Check(source, usages);
    }

    [Fact]
    public void Type_Generic()
    {
        const string source =
            """
            using System;
            static class Test {
                static void M(Func<string, int> p) {}
            }
            """;

        var usages = new Dictionary<string, int>
        {
            { "T:System.Object", 1 }, // Base
            { "T:System.Void", 1 },
            { "T:System.Func`2", 1 },
            { "T:System.String", 1 },
            { "T:System.Int32", 1 },
        };

        Check(source, usages);
    }

    [Fact]
    public void Type_Array()
    {
        const string source =
            """
            using System;
            static class Test {
                static void M(string[] p) {}
            }
            """;

        var usages = new Dictionary<string, int>
        {
            { "T:System.Object", 1 }, // Base
            { "T:System.Void", 1 },
            { "T:System.String", 1 }
        };

        Check(source, usages);
    }

    // TODO: Pointers

    private static void Check(string source, IReadOnlyDictionary<string, int> expectedResults)
    {
        var assembly = Compiler.Compile(source);
        Check(assembly, expectedResults);
    }

    private static void Check(IAssembly assembly, IReadOnlyDictionary<string, int> expectedResultsText)
    {
        var expectedResults = expectedResultsText.ToDictionary(kv => new ApiKey(kv.Key), kv => kv.Value);

        var crawler = new AssemblyCrawler();
        crawler.Crawl(assembly);

        var actualResults = crawler.GetResults().Data;

        var messageBuilder = new StringBuilder();

        foreach (var key in expectedResults.Keys.Where(k => actualResults.ContainsKey(k) && expectedResults[k] != actualResults[k]))
        {
            if (Compiler.IsAutoGenerated(key.DocumentationId))
                continue;

            messageBuilder.AppendLine($"{key} was expected to have a value {expectedResults[key]} but was {actualResults[key]}.");
        }

        foreach (var key in expectedResults.Keys.Where(k => !actualResults.ContainsKey(k)))
        {
            if (Compiler.IsAutoGenerated(key.DocumentationId))
                continue;

            messageBuilder.AppendLine($"{key} was expected but is missing.");
        }

        foreach (var key in actualResults.Keys.Where(k => !expectedResults.ContainsKey(k)))
        {
            if (Compiler.IsAutoGenerated(key.DocumentationId))
                continue;

            messageBuilder.AppendLine($"{key} was not expected.");
        }

        if (messageBuilder.Length > 0)
            throw new Exception(messageBuilder.ToString());
    }
}