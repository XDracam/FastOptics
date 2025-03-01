using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using Verifier =
    Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
        DracTec.Optics.Generators.LensAnalyzer, 
        Microsoft.CodeAnalysis.Testing.DefaultVerifier
    >;

namespace DracTec.Optics.Generators.Tests;

public class CustomTest : CSharpAnalyzerTest<LensAnalyzer, DefaultVerifier>
{
    public static CustomTest Create(string text) => new()
    {
        TestCode = text,
        SolutionTransforms =
        {
            // Add references to local projects
            (solution, projectId) =>
            {
                var project = solution.GetProject(projectId);
                project = project!.AddMetadataReference(
                    MetadataReference.CreateFromFile(typeof(ILens<,>).Assembly.Location));
                return project.Solution;
            }
        }
    };
}

public class LensAnalyzerTests
{
    [Fact]
    public async Task NoILensReturnType_ErrorDiagnostic()
    {
        const string text = 
            """
            using DracTec.Optics;
            
            public static partial class TestClass {
                [Lens(".Name.First")]
                public static partial string FirstNameLens();
                
                public static partial string FirstNameLens() => "no generation due to invalid result";
            }
            """;

        var test = CustomTest.Create(text);

        var expected = Verifier.Diagnostic(LensAnalyzer.ReturnTypeDiagnostic.Rule)
            .WithSeverity(DiagnosticSeverity.Error).WithSpan(4, 6, 4, 25);
        test.ExpectedDiagnostics.Add(expected);
        await test.RunAsync(CancellationToken.None);
    }
    
    [Fact]
    public async Task InvalidLensResultType_ErrorDiagnostic()
    {
        const string text = 
            """
            using DracTec.Optics;
            
            namespace System.Runtime.CompilerServices {
                public static class IsExternalInit { }
            }
            
            public sealed record Name(string First, string Last);
            public sealed record Person(Name Name, int Age);

            public static partial class TestClass {
                [Lens(".Name.Last")]
                public static partial ILens<Person, int> LastNameLens();
                
                public static partial ILens<Person, int> LastNameLens() => default; // "no generation in this test";
            }
            """;

        var test = CustomTest.Create(text);

        var expected = Verifier.Diagnostic(LensAnalyzer.ValidPathDiagnostic.Rule).WithSeverity(DiagnosticSeverity.Error)
            .WithSpan(11, 11, 11, 23).WithArguments(".Name.Last", "Person", "int", "it has type 'string'");
        test.ExpectedDiagnostics.Add(expected);
        await test.RunAsync(CancellationToken.None);
    }
    
    [Fact]
    public async Task InvalidPath_ErrorDiagnostic()
    {
        const string text = 
            """
            using DracTec.Optics;
            
            namespace System.Runtime.CompilerServices {
                public static class IsExternalInit { }
            }

            public sealed record Name(string First, string Last);
            public sealed record Person(Name Name, int Age);

            public static partial class TestClass {
                [Lens(".Name.Blast")]
                public static partial ILens<Person, string> LastNameLens();
                
                public static partial ILens<Person, string> LastNameLens() => default; // "no generation in this test"
            }
            """;

        var test = CustomTest.Create(text);

        var expected = Verifier.Diagnostic(LensAnalyzer.ValidPathDiagnostic.Rule).WithSeverity(DiagnosticSeverity.Error)
            .WithSpan(11, 11, 11, 24).WithArguments(".Name.Blast", "Person", "string", "the expression does not compile");
        test.ExpectedDiagnostics.Add(expected);
        await test.RunAsync(CancellationToken.None);
    }
}