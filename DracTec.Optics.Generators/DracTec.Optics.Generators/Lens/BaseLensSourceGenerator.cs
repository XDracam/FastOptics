using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace DracTec.Optics.Generators;

// For any partial declaration of type TSyntax with [Lens(".Some.Path")], generates an implementation
//  for that declaration that returns a singleton ILens<A, B> for the expression in the attribute.

public abstract class BaseLensSourceGenerator<TSyntax> : IIncrementalGenerator where TSyntax : SyntaxNode
{
    private const string AttributeName = "Lens";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Filter classes annotated with the [WithLenses] attribute.
        // Only filtered Syntax Nodes can trigger code generation.
        var provider = context.SyntaxProvider
            .CreateSyntaxProvider(
                (s, _) => s is TSyntax,
                (ctx, _) => GetDeclarationForSourceGen(ctx))
            .Where(t => t != null)
            .Select((t, _) => t!.Value);

        context.RegisterSourceOutput(
            context.CompilationProvider.Combine(provider.Collect()),
            (ctx, t) => GenerateCode(ctx, t.Left, t.Right)
        );
    }

    protected abstract IEnumerable<AttributeListSyntax> getAttributeLists(TSyntax syntax);
    
    private (TSyntax syntax, string path)? GetDeclarationForSourceGen(GeneratorSyntaxContext context)
    {
        var declarationSyntax = (TSyntax)context.Node;

        foreach (AttributeListSyntax attributeListSyntax in getAttributeLists(declarationSyntax))
        foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
        {
            var attributeName = attributeSyntax.Name.ToString();
            
            if (attributeName == AttributeName)
            {
                var expression =
                    attributeSyntax.ArgumentList?.Arguments.SingleOrDefault()?.Expression as LiteralExpressionSyntax;
                var path = expression?.Token.ValueText;
                if (path == null) return null;
                return (declarationSyntax, path);
            }
        }

        return null;
    }

    protected abstract GenericNameSyntax? getReturnTypeSyntax(TSyntax syntax);
    protected abstract ISymbol? getDeclarationSymbol(SemanticModel semanticModel, TSyntax syntax);

    protected abstract string getDeclarationIdentifier(TSyntax syntax);
    protected abstract string getImplementation(TSyntax syntax, string lensSingletonName);
    
    private void GenerateCode(SourceProductionContext context, Compilation compilation,
        ImmutableArray<(TSyntax syntax, string path)> methodDeclarations)
    {
        foreach (var (declarationSyntax, path) in methodDeclarations)
        {
            // return type must be ILens<inputType, outputType>
            if (getReturnTypeSyntax(declarationSyntax) is not { Identifier.Text: "ILens" } lensSyntax) continue;
            var arguments = lensSyntax.TypeArgumentList.Arguments;
            if (arguments.Count != 2) continue;

            var inputTypeSyntax = arguments[0];
            var outputTypeSyntax = arguments[1];
            
            var semanticModel = compilation.GetSemanticModel(declarationSyntax.SyntaxTree);
            if (getDeclarationSymbol(semanticModel, declarationSyntax) is not { } declarationSymbol) continue;

            // ignore indent, we just use auto-formatting
            var (pre, _, post) = CodeGenerationUtils.MkContext(
                declarationSyntax.Parent, 
                ["DracTec.Optics", "System.Runtime.CompilerServices"], 
                includeSelf: true
            );

            var lensTypeName = inputTypeSyntax + path.Replace(".", "_") + "_Lens";
            var lensSingletonName = getDeclarationIdentifier(declarationSyntax) + "_LensInstance";

            var implementation = getImplementation(declarationSyntax, lensSingletonName);

            var codeBuilder = new StringBuilder();
            codeBuilder.AppendLine(pre);
            codeBuilder.AppendLine($$"""
                                     private static partial class LensImplementations {
                                         private sealed class {{lensTypeName}} : ILens<{{inputTypeSyntax}}, {{outputTypeSyntax}}> {
                                             public {{outputTypeSyntax}} Get({{inputTypeSyntax}} theRecord) => theRecord{{path}};
                                             public {{inputTypeSyntax}} Set({{inputTypeSyntax}} theRecord, {{outputTypeSyntax}} value) =>
                                                 theRecord with { {{setPropChain(path)}} };   
                                         }
                                         
                                         public static readonly {{lensSyntax}} {{lensSingletonName}} = new {{lensTypeName}}();
                                     }
                                        
                                     {{implementation}}
                                     """);
            codeBuilder.Append(post);

            var name = CodeGenerationUtils.UniqueFileNameFor(declarationSymbol);
            
            // Auto-format output for consistency
            var syntaxTree = CSharpSyntaxTree.ParseText(codeBuilder.ToString());
            var root = syntaxTree.GetRoot().NormalizeWhitespace();

            context.AddSource($"{name}.Lenses.g.cs", SourceText.From(root.ToFullString(), Encoding.UTF8));
        }
        
        static string setPropChain(string path)
        {
            var props = path.Split('.').Skip(1).ToImmutableArray();
            return string.Join("", Enumerable.Range(0, props.Length - 1).Select(i =>
                       $"{props[i]} = theRecord.{string.Join(".", props.Take(i + 1).Select(p => p))} with {{"))
                   + $" {props.Last()} = value "
                   + new string('}', props.Length - 1);
        }
    }
}