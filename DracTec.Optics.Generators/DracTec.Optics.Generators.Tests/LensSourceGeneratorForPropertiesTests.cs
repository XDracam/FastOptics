using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace DracTec.Optics.Generators.Tests;

public class LensSourceGeneratorForPropertiesTests
{
    private const string SourceText = 
        """
        namespace TestNamespace;

        public record Name(string First, string Last);
        public record struct Person(Name Name, int Age);
        
        public partial static class TestClass { 
            [Lens(".Name.First")]
            public static partial ILens<Person, string> FirstNameLens { get; }
        }
        """;

    private const string ExpectedGeneratedText = 
        """
        using DracTec.Optics;
        using System.Runtime.CompilerServices;

        namespace TestNamespace;

        public partial static class TestClass
        {    
            private static partial class LensImplementations {
                private sealed class Person_Name_First_Lens : ILens<Person, string> { 
                    public string Get(Person theRecord) => theRecord.Name.First;
                    public Person Set(Person theRecord, string value) => 
                        theRecord with { Name = theRecord.Name with { First = value }};
                }
                
                public static readonly ILens<Person, string> FirstNameLens_LensInstance = new Person_Name_First_Lens();
            }
            
            public static partial ILens<Person, string> FirstNameLens => LensImplementations.FirstNameLens_LensInstance;
        }
        """;

    [Fact]
    public void GenerateReportMethod()
    {
        var generator = new LensSourceGeneratorForProperties();

        var driver = CSharpGeneratorDriver.Create(generator);

        var compilation = CSharpCompilation.Create(nameof(LensSourceGeneratorForPropertiesTests),
            [CSharpSyntaxTree.ParseText(SourceText)],
            [
                // To support 'System.Attribute' inheritance, add reference to 'System.Private.CoreLib'.
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
            ]);

        var runResult = driver.RunGenerators(compilation).GetRunResult();

        var generatedFileSyntax = runResult.GeneratedTrees.Single(t => t.FilePath.EndsWith("FirstNameLens.Lenses.g.cs"));

        var expectedFormatted = CSharpSyntaxTree
            .ParseText(ExpectedGeneratedText)
            .GetRoot()
            .NormalizeWhitespace()
            .ToFullString();

        Assert.Equal(expectedFormatted, generatedFileSyntax.GetText().ToString(),
            ignoreLineEndingDifferences: true);
    }
}