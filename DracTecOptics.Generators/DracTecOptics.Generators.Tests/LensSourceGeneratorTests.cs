using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace DracTecOptics.Generators.Tests;

public class LensSourceGeneratorTests
{
    private const string SourceText = 
        """
        namespace TestNamespace;

        public record Name(string First, string Last);

        [WithLenses]
        public partial record Person(Name Name, int Age);
        """;

    private const string ExpectedGeneratedText = 
        """
        using DracTec.Optics;
        using System.Runtime.CompilerServices;

        namespace TestNamespace;

        public partial record Person
        {    
            public static class Lens
            {
                public static LensFor_Name Name => LensFor_Name.Instance;
                public static LensFor_Age Age => LensFor_Age.Instance;
            }
            
            public sealed class LensFor_Name_First : ILens<Person, string>
            {
                private LensFor_Name_First() { }
                public static readonly LensFor_Name_First Instance = new();
                
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public string Get(Person theRecord) => theRecord.Name.First;
                public Person Set(Person theRecord, string value) => 
                    theRecord with { Name = theRecord.Name with { First = value } };
            }
            
            public sealed class LensFor_Name_Last : ILens<Person, string>
            {
                private LensFor_Name_Last() { }
                public static readonly LensFor_Name_Last Instance = new();
                
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public string Get(Person theRecord) => theRecord.Name.Last;
                public Person Set(Person theRecord, string value) => 
                    theRecord with { Name = theRecord.Name with { Last = value } };
            }
            
            public sealed class LensFor_Name : ILens<Person, global::TestNamespace.Name>
            {
                private LensFor_Name() { }
                public static readonly LensFor_Name Instance = new();
                
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public global::TestNamespace.Name Get(Person theRecord) => theRecord.Name;
                public Person Set(Person theRecord, global::TestNamespace.Name value) => theRecord with { Name = value };
                
                public LensFor_Name_First First => LensFor_Name_First.Instance;
                public LensFor_Name_Last Last => LensFor_Name_Last.Instance;
            }
            
            public sealed class LensFor_Age : ILens<Person, int>
            {
                private LensFor_Age() { }
                public static readonly LensFor_Age Instance = new();
                
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public int Get(Person theRecord) => theRecord.Age;
                public Person Set(Person theRecord, int value) => theRecord with { Age = value };
            }
        }
        """;

    [Fact]
    public void GenerateReportMethod()
    {
        var generator = new LensSourceGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);

        var compilation = CSharpCompilation.Create(nameof(LensSourceGeneratorTests),
            [CSharpSyntaxTree.ParseText(SourceText)],
            [
                // To support 'System.Attribute' inheritance, add reference to 'System.Private.CoreLib'.
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
            ]);

        var runResult = driver.RunGenerators(compilation).GetRunResult();

        var generatedFileSyntax = runResult.GeneratedTrees.Single(t => t.FilePath.EndsWith("Person.Lenses.g.cs"));

        var expectedFormatted = CSharpSyntaxTree
            .ParseText(ExpectedGeneratedText)
            .GetRoot()
            .NormalizeWhitespace()
            .ToFullString();

        Assert.Equal(expectedFormatted, generatedFileSyntax.GetText().ToString(),
            ignoreLineEndingDifferences: true);
    }
}