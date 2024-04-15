﻿using Basic.Reference.Assemblies;
using Microsoft.Cci;
using Microsoft.Cci.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Terrajobst.UsageCrawling.Tests.Infra;

internal static class Compiler
{
    public static IAssembly Compile(string source)
    {
        return Compile(source, c => c);
    }
    
    public static IAssembly Compile(string source, Func<CSharpCompilation, CSharpCompilation> modifyCompilation)
    {
        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                                                  optimizationLevel: OptimizationLevel.Release);
        var compilation = CSharpCompilation.Create("dummy",
                                                   [ CSharpSyntaxTree.ParseText(source) ],
                                                   Net80.References.All,
                                                   options);

        compilation = modifyCompilation(compilation);

        using var peStream = new MemoryStream();
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

    public static bool IsAutoGenerated(string documentationId)
    {
        ThrowIfNull(documentationId);

        return AutoGeneratedKeys.Contains(documentationId);
    }

    private static readonly SortedSet<string> AutoGeneratedKeys = [
        "T:System.Runtime.CompilerServices.CompilerGeneratedAttribute",
        "M:System.Runtime.CompilerServices.CompilerGeneratedAttribute.#ctor",
        "T:System.Runtime.CompilerServices.CompilationRelaxationsAttribute",
        "M:System.Runtime.CompilerServices.CompilationRelaxationsAttribute.#ctor(System.Int32)",
        "T:System.Runtime.CompilerServices.RuntimeCompatibilityAttribute",
        "M:System.Runtime.CompilerServices.RuntimeCompatibilityAttribute.#ctor",
        "F:System.Runtime.CompilerServices.RuntimeCompatibilityAttribute.WrapNonExceptionThrows",
        "P:System.Runtime.CompilerServices.RuntimeCompatibilityAttribute.WrapNonExceptionThrows",
        "T:System.Runtime.CompilerServices.RefSafetyRulesAttribute",
        "M:System.Runtime.CompilerServices.RefSafetyRulesAttribute.#ctor(System.Int32)",
        "T:System.Diagnostics.DebuggableAttribute",
        "M:System.Diagnostics.DebuggableAttribute.#ctor(System.Diagnostics.DebuggableAttribute.DebuggingModes)"
    ];

}