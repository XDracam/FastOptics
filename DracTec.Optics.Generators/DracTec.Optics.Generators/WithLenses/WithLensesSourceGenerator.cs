using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace DracTec.Optics.Generators;

// Generates specialized lens implementations for all properties on all records with [WithLenses]
// Relevant design choices:
// 1. The generated lenses have the naming scheme `LensFor_Property1_..._PropertyN` to be unique and human-readable.
// 2. The top level `Lens` static class exists to avoid naming conflicts,
//     e.g. a Person cannot have a property `Name` as well as a static property called `Name` at the same time.
// 3. The properties contain the specific types to allow nested properties, e.g. `Person.Lens.Name.First`.
// 4. Nested lenses could be done with nested static classes and generated static Get and Set methods, but:
//    - massively increased complexity in the generation code
//    - using lenses as values would be worse, requiring an explicit `.Instance` call
//    - more generated code means more work for the compiler
// 5. Lenses are singleton classes with private constructors *instead* of structs
//     to avoid boxing when using them as `ILens`es.
// 6. The `Instance` field is always eagerly initialized -> basically no performance overhead

[Generator]
public class WithLensesSourceGenerator : IIncrementalGenerator
{
    private const string AttributeName = "WithLenses";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Filter classes annotated with the [WithLenses] attribute.
        // Only filtered Syntax Nodes can trigger code generation.
        var provider = context.SyntaxProvider
            .CreateSyntaxProvider(
                (s, _) => s is RecordDeclarationSyntax,
                (ctx, _) => GetRecordDeclarationForSourceGen(ctx))
            .Where(t => t != null)
            .Select((t, _) => t!.Value);

        context.RegisterSourceOutput(
            context.CompilationProvider.Combine(provider.Collect()),
            (ctx, t) => GenerateCode(ctx, t.Left, t.Right)
        );
    }
    
    private static (RecordDeclarationSyntax syntax, bool isRecursive)? GetRecordDeclarationForSourceGen(
        GeneratorSyntaxContext context)
    {
        var recordDeclarationSyntax = (RecordDeclarationSyntax)context.Node;

        foreach (AttributeListSyntax attributeListSyntax in recordDeclarationSyntax.AttributeLists)
        foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
        {
            var attributeName = attributeSyntax.Name.ToString();
            
            if (attributeName == AttributeName)
            {
                // is recursive when the attribute syntax has no constructor or it has a "true" parameter
                var isRecursive = attributeSyntax.ArgumentList?.Arguments
                    .Any(a => a.Expression is LiteralExpressionSyntax { Token.ValueText: "true" }) ?? true;
                return (recordDeclarationSyntax, isRecursive);
            }
        }

        return null;
    }
    
    private static void GenerateCode(SourceProductionContext context, Compilation compilation,
        ImmutableArray<(RecordDeclarationSyntax syntax, bool isRecurive)> recordDeclarations)
    {
        foreach (var (recordDeclarationSyntax, isRecursive) in recordDeclarations)
        {
            var semanticModel = compilation.GetSemanticModel(recordDeclarationSyntax.SyntaxTree);
            if (semanticModel.GetDeclaredSymbol(recordDeclarationSyntax) is not { } recordSymbol)
                continue;

            // ignore indent, we just use auto-formatting
            var (pre, _, post) = CodeGenerationUtils.MkContext(
                recordDeclarationSyntax, 
                ["DracTec.Optics", "System.Runtime.CompilerServices"], 
                includeSelf: true
            );

            var properties = recordSymbol.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p => p.SetMethod != null)
                .ToImmutableArray();

            var lenses = properties
                .SelectMany(p => GenerateLenses(recordSymbol, ImmutableList.Create(p), isRecursive));

            var codeBuilder = new StringBuilder();
            codeBuilder.AppendLine(pre);
            codeBuilder.AppendLine($$"""
                                     public static class Lens {
                                     {{string.Join("\n", properties.Select(p => 
                                         $"    public static Generated.LensFor_{p.Name} {p.Name} => " +
                                                $"Generated.LensFor_{p.Name}.Instance;"))}}
                                     """);
            codeBuilder.AppendLine("public static class Generated {");
            foreach (var lens in lenses)
                codeBuilder.AppendLine(lens);
            codeBuilder.AppendLine("}}"); // close Generated and Lens
            codeBuilder.Append(post);

            var name = CodeGenerationUtils.UniqueFileNameFor(recordSymbol);
            
            // Auto-format output for consistency
            var syntaxTree = CSharpSyntaxTree.ParseText(codeBuilder.ToString());
            var root = syntaxTree.GetRoot().NormalizeWhitespace();

            context.AddSource($"{name}.Lenses.g.cs", SourceText.From(root.ToFullString(), Encoding.UTF8));
        }
    }

    private static IEnumerable<string> GenerateLenses(
        INamedTypeSymbol topLevelRecord, 
        ImmutableList<IPropertySymbol> properties, // in order of callchain
        bool isRecursive)
    {
        var currentProp = properties.Last();

        var propertiesToReference = new List<string>();

        // if currentProp is a record, generate lenses for its properties
        if (isRecursive && currentProp.Type is INamedTypeSymbol { IsRecord: true } recordType)
        {
            foreach (var property in recordType.GetMembers()
                         .OfType<IPropertySymbol>()
                         .Where(p => p.SetMethod != null))
            {
                propertiesToReference.Add(
                    $"    public LensFor_{string.Join("_", properties.Add(property).Select(p => p.Name))} {property.Name} => " +
                    $"LensFor_{string.Join("_", properties.Add(property).Select(p => p.Name))}.Instance;");
                
                foreach (var lens in GenerateLenses(topLevelRecord, properties.Add(property), isRecursive))
                    yield return lens;
            }
        }
        
        var name = "LensFor_" + string.Join("_", properties.Select(p => p.Name));
        var recordName = topLevelRecord.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        var propertyType = properties.Last().Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        yield return $@"public sealed class {name} : ILens<{recordName}, {propertyType}>
{{
    private {name}() {{ }}
    public static readonly {name} Instance = new();
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public {propertyType} Get({recordName} theRecord) => 
        theRecord{string.Join("", properties.Select(p => $".{p.Name}"))};
    public {recordName} Set({recordName} theRecord, {propertyType} value) => 
        theRecord with {{ {setPropChain(properties)} }};
{string.Join("\n", propertiesToReference)}
}}"; 
        static string setPropChain(ImmutableList<IPropertySymbol> properties) =>
            string.Join("", Enumerable.Range(0, properties.Count - 1).Select(i => 
                $"{properties[i].Name} = theRecord.{string.Join(".", properties.Take(i + 1).Select(p => p.Name))} with {{"))
            + $" {properties.Last().Name} = value "
            + new string('}', properties.Count - 1);
    }
}