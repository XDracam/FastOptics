using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DracTec.Optics.Generators;

// Implements a partial method with a [Lens(".Foo.Bar")] attribute.
// This generates a lens class and a singleton instance and implements the method to return that instance.

[Generator]
public class LensSourceGeneratorForMethods : BaseLensSourceGenerator<MethodDeclarationSyntax>
{
    protected override IEnumerable<AttributeListSyntax> getAttributeLists(MethodDeclarationSyntax syntax) => 
        syntax.AttributeLists;

    protected override GenericNameSyntax? getReturnTypeSyntax(MethodDeclarationSyntax syntax) =>
        syntax.ReturnType as GenericNameSyntax; 

    protected override ISymbol? getDeclarationSymbol(SemanticModel semanticModel, MethodDeclarationSyntax syntax) =>
        semanticModel.GetDeclaredSymbol(syntax);

    protected override string getDeclarationIdentifier(MethodDeclarationSyntax syntax) => syntax.Identifier.Text;

    protected override string getImplementation(MethodDeclarationSyntax syntax, string lensSingletonName)
    {
        var implementation = syntax
            .WithExpressionBody(
                SyntaxFactory.ArrowExpressionClause(
                    SyntaxFactory.ParseExpression("LensImplementations." + lensSingletonName)))
            .WithAttributeLists(SyntaxFactory.List<AttributeListSyntax>());

        if (!implementation.Modifiers.Any(SyntaxKind.PartialKeyword))
            implementation = implementation.AddModifiers(SyntaxFactory.Token(SyntaxKind.PartialKeyword));

        return implementation.ToString();
    }
}