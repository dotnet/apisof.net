using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Cci;
using Microsoft.Cci.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Terrajobst.UsageCrawling.Tests;

public class AssemblyCrawlerTests
{
    [Fact]
    public void Attributes_Assembly()
    {
        const string Source = @"
            using System.Reflection;
            [assembly: AssemblyMetadata(""key"", ""value"")]
        ";

        var usages = new Dictionary<string, int>
        {
            { "T:System.Reflection.AssemblyMetadataAttribute", 1 },
            { "M:System.Reflection.AssemblyMetadataAttribute.#ctor(System.String,System.String)", 1 }
        };

        Check(Source, usages);
    }

    [Fact]
    public void Attributes_Module()
    {
        const string Source = @"
            using System;
            [module: CLSCompliant(false)]
        ";

        var usages = new Dictionary<string, int>
        {
            { "T:System.CLSCompliantAttribute", 1 },
            { "M:System.CLSCompliantAttribute.#ctor(System.Boolean)", 1 }
        };

        Check(Source, usages);
    }

    [Fact]
    public void Attributes_Type()
    {
        const string Source = @"
            using System;
            [Obsolete(DiagnosticId = ""x"")]
            class Test { }
        ";

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

        Check(Source, usages);
    }

    [Fact]
    public void Attributes_Constructor()
    {
        const string Source = @"
            using System;
            class Test {
                [Obsolete]
                Test() { }
            }
        ";

        var usages = new Dictionary<string, int>
        {
            { "T:System.Object", 1 },       // Base
            { "M:System.Object.#ctor", 1 }, // Base call
            { "T:System.Void", 1 },         // Constructor
            { "T:System.ObsoleteAttribute", 1 },
            { "M:System.ObsoleteAttribute.#ctor", 1 }
        };

        Check(Source, usages);
    }

    [Fact]
    public void Attributes_Method()
    {
        const string Source = @"
            using System;
            class Test {
                [Obsolete]
                void DoStuff() { }
            }
        ";

        var usages = new Dictionary<string, int>
        {
            { "T:System.Object", 1 },       // Base
            { "M:System.Object.#ctor", 1 }, // Base call
            { "T:System.Void", 2 },         // Constructor + method
            { "T:System.ObsoleteAttribute", 1 },
            { "M:System.ObsoleteAttribute.#ctor", 1 }
        };

        Check(Source, usages);
    }

    [Fact]
    public void Attributes_Field()
    {
        const string Source = @"
            using System;
            class Test {
                [Obsolete]
                int _member;
            }
        ";

        var usages = new Dictionary<string, int>
        {
            { "T:System.Int32", 1 },
            { "T:System.Object", 1 },       // Base
            { "M:System.Object.#ctor", 1 }, // Base call
            { "T:System.Void", 1 },         // Constructor
            { "T:System.ObsoleteAttribute", 1 },
            { "M:System.ObsoleteAttribute.#ctor", 1 }
        };

        Check(Source, usages);
    }

    [Fact]
    public void Attributes_Property()
    {
        const string Source = @"
            using System;
            class Test {
                [Obsolete]
                int P { get; set; }
            }
        ";

        var usages = new Dictionary<string, int>
        {
            { "T:System.Int32", 4 },        // Property + getter + setter + field
            { "T:System.Object", 1 },       // Base
            { "M:System.Object.#ctor", 1 }, // Base call
            { "T:System.Void", 2 },         // Constructor + setter
            { "T:System.ObsoleteAttribute", 1 },
            { "M:System.ObsoleteAttribute.#ctor", 1 }
        };

        Check(Source, usages);
    }

    [Fact]
    public void Attributes_Event()
    {
        const string Source = @"
            using System;
            class Test {
                [Obsolete]
                event EventHandler E;
            }
        ";

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

        Check(Source, usages);
    }

    [Fact]
    public void Type_BaseType()
    {
        const string Source = @"
            class Test { }
        ";

        var usages = new Dictionary<string, int>
        {
            { "T:System.Object", 1 },       // Base
            { "M:System.Object.#ctor", 1 }, // Base call
            { "T:System.Void", 1 },         // Constructor
        };

        Check(Source, usages);
    }

    [Fact]
    public void Type_InterfaceImplementation()
    {
        const string Source = @"
            using System.Collections;
            class Test : IEnumerable {
                public IEnumerator GetEnumerator() => null;
            }
        ";

        var usages = new Dictionary<string, int>
        {
            { "T:System.Object", 1 },       // Base
            { "M:System.Object.#ctor", 1 }, // Base call
            { "T:System.Collections.IEnumerable", 1 },
            { "T:System.Collections.IEnumerator", 1 },
            { "T:System.Void", 1 }          // Constructor
        };

        Check(Source, usages);
    }

    [Fact]
    public void Method()
    {
        const string Source = @"
            using System;
            class Test {
                public void M() {
                    int.Parse(""x"");
                }
            }
        ";

        var usages = new Dictionary<string, int>
        {
            { "T:System.Object", 1 },       // Base
            { "M:System.Object.#ctor", 1 }, // Base call
            { "T:System.Void", 2 },         // Constructor + method
            { "M:System.Int32.Parse(System.String)", 1 }
        };

        Check(Source, usages);
    }

    [Fact]
    public void Property_Get()
    {
        const string Source = @"
            using System;
            class Test {
                public void M() {
                    var x = ""x"".Length;
                }
            }
        ";

        var usages = new Dictionary<string, int>
        {
            { "T:System.Object", 1 },       // Base
            { "M:System.Object.#ctor", 1 }, // Base call
            { "T:System.Void", 2 },         // Constructor + method
            { "M:System.String.get_Length", 1 }
        };

        Check(Source, usages);
    }

    [Fact]
    public void Property_Set()
    {
        const string Source = @"
            using System;
            class Test {
                public void M() {
                    new ObsoleteAttribute {
                        DiagnosticId = ""x""
                    };
                }
            }
        ";

        var usages = new Dictionary<string, int>
        {
            { "T:System.Object", 1 },       // Base
            { "M:System.Object.#ctor", 1 }, // Base call
            { "T:System.Void", 2 },         // Constructor + method
            { "M:System.ObsoleteAttribute.#ctor", 1 },
            { "M:System.ObsoleteAttribute.set_DiagnosticId(System.String)", 1 }
        };

        Check(Source, usages);
    }

    [Fact]
    public void Event_Add()
    {
        const string Source = @"
            using System;
            static class Test {
                static void M() {
                    AppDomain.CurrentDomain.UnhandledException += Handler;
                }
                static void Handler(object args, UnhandledExceptionEventArgs e) {}
            }
        ";

        var usages = new Dictionary<string, int>
        {
            { "T:System.Object", 2 }, // Base, event handler sender
            { "T:System.Void", 2 },
            { "M:System.AppDomain.get_CurrentDomain", 1 },
            { "M:System.UnhandledExceptionEventHandler.#ctor(System.Object,System.IntPtr)", 1 },
            { "M:System.AppDomain.add_UnhandledException(System.UnhandledExceptionEventHandler)", 1 },
            { "T:System.UnhandledExceptionEventArgs", 1 }
        };

        Check(Source, usages);
    }

    [Fact]
    public void Event_Remove()
    {
        const string Source = @"
            using System;
            static class Test {
                static void M() {
                    AppDomain.CurrentDomain.UnhandledException -= Handler;
                }
                static void Handler(object args, UnhandledExceptionEventArgs e) {}
            }
        ";

        var usages = new Dictionary<string, int>
        {
            { "T:System.Object", 2 }, // Base, event handler sender
            { "T:System.Void", 2 },
            { "M:System.AppDomain.get_CurrentDomain", 1 },
            { "M:System.UnhandledExceptionEventHandler.#ctor(System.Object,System.IntPtr)", 1 },
            { "M:System.AppDomain.remove_UnhandledException(System.UnhandledExceptionEventHandler)", 1 },
            { "T:System.UnhandledExceptionEventArgs", 1 }
        };

        Check(Source, usages);
    }

    [Fact]
    public void Field_Read()
    {
        const string Source = @"
            using System.IO;
            static class Test {
                static void M() {
                    var x = Path.PathSeparator;
                }
            }
        ";

        var usages = new Dictionary<string, int>
        {
            { "T:System.Object", 1 }, // Base
            { "T:System.Void", 1 },
            { "F:System.IO.Path.PathSeparator", 1 }
        };

        Check(Source, usages);
    }

    [Fact]
    public void Field_Write()
    {
        const string Source = @"
            using System.Runtime.InteropServices;
            static class Test {
                static void M() {
                    new MarshalAsAttribute((short)0) { MarshalType = """" };
                }
            }
        ";

        var usages = new Dictionary<string, int>
        {
            { "T:System.Object", 1 }, // Base
            { "T:System.Void", 1 },
            { "M:System.Runtime.InteropServices.MarshalAsAttribute.#ctor(System.Int16)", 1 },
            { "F:System.Runtime.InteropServices.MarshalAsAttribute.MarshalType", 1 }
        };

        Check(Source, usages);
    }

    [Fact]
    public void Type_Generic()
    {
        const string Source = @"
            using System;
            static class Test {
                static void M(Func<string, int> p) {}
            }
        ";

        var usages = new Dictionary<string, int>
        {
            { "T:System.Object", 1 }, // Base
            { "T:System.Void", 1 },
            { "T:System.Func`2", 1 },
            { "T:System.String", 1 },
            { "T:System.Int32", 1 },
        };

        Check(Source, usages);
    }

    [Fact]
    public void Type_Array()
    {
        const string Source = @"
            using System;
            static class Test {
                static void M(string[] p) {}
            }
        ";

        var usages = new Dictionary<string, int>
        {
            { "T:System.Object", 1 }, // Base
            { "T:System.Void", 1 },
            { "T:System.String", 1 }
        };

        Check(Source, usages);
    }

    // TODO: Pointers

    private static void Check(string source, IReadOnlyDictionary<string, int> expectedResults)
    {
        var assembly = CompileAssembly(source);
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
            if (_autoGeneratedKeys.Contains(key.DocumentationId))
                continue;

            messageBuilder.AppendLine($"{key} was expected to have a value {expectedResults[key]} but was {actualResults[key]}.");
        }

        foreach (var key in expectedResults.Keys.Where(k => !actualResults.ContainsKey(k)))
        {
            if (_autoGeneratedKeys.Contains(key.DocumentationId))
                continue;

            messageBuilder.AppendLine($"{key} was expected but is missing.");
        }

        foreach (var key in actualResults.Keys.Where(k => !expectedResults.ContainsKey(k)))
        {
            if (_autoGeneratedKeys.Contains(key.DocumentationId))
                continue;

            messageBuilder.AppendLine($"{key} was not expected.");
        }

        if (messageBuilder.Length > 0)
            throw new Exception(messageBuilder.ToString());
    }

    private static IAssembly CompileAssembly(string source)
    {
        var corlibPath = typeof(object).Assembly.Location;
        var corlibReference = MetadataReference.CreateFromFile(corlibPath);
        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                                                   optimizationLevel: OptimizationLevel.Release);
        var compilation = CSharpCompilation.Create("dummy",
                                                   new[] { CSharpSyntaxTree.ParseText(source) },
                                                   new[] { corlibReference },
                                                   options);
        var peStream = new MemoryStream();
        var result = compilation.Emit(peStream);
        if (!result.Success)
        {
            var diagnostics = string.Join(Environment.NewLine, result.Diagnostics);
            var message = $"Compilation has errors{Environment.NewLine}{diagnostics}";
            throw new Exception(message);
        }

        peStream.Position = 0;

        var environment = new HostEnvironment();
        var assembly = environment.LoadAssemblyFrom(compilation.AssemblyName, peStream);
        return assembly;
    }

    private static readonly SortedSet<string> _autoGeneratedKeys = new()
    {
        "T:System.Runtime.CompilerServices.CompilerGeneratedAttribute",
        "M:System.Runtime.CompilerServices.CompilerGeneratedAttribute.#ctor",
        "T:System.Runtime.CompilerServices.CompilationRelaxationsAttribute",
        "M:System.Runtime.CompilerServices.CompilationRelaxationsAttribute.#ctor(System.Int32)",
        "T:System.Runtime.CompilerServices.RuntimeCompatibilityAttribute",
        "M:System.Runtime.CompilerServices.RuntimeCompatibilityAttribute.#ctor",
        "F:System.Runtime.CompilerServices.RuntimeCompatibilityAttribute.WrapNonExceptionThrows",
        "P:System.Runtime.CompilerServices.RuntimeCompatibilityAttribute.WrapNonExceptionThrows",
        "T:System.Diagnostics.DebuggableAttribute",
        "M:System.Diagnostics.DebuggableAttribute.#ctor(System.Diagnostics.DebuggableAttribute.DebuggingModes)",
    };
}
