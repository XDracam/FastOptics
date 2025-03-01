using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DracTec.Optics.Generators;

// Implements a partial get-only property with a [Lens(".Foo.Bar")] attribute.
// This generates a lens class and a singleton instance and implements the property to return that instance.

[Generator]
public class LensSourceGeneratorForProperties : BaseLensSourceGenerator<PropertyDeclarationSyntax>
{
    protected override IEnumerable<AttributeListSyntax> getAttributeLists(PropertyDeclarationSyntax syntax) => 
        syntax.AttributeLists;

    protected override GenericNameSyntax? getReturnTypeSyntax(PropertyDeclarationSyntax syntax) =>
        syntax.Type as GenericNameSyntax; 

    protected override ISymbol? getDeclarationSymbol(SemanticModel semanticModel, PropertyDeclarationSyntax syntax) =>
        semanticModel.GetDeclaredSymbol(syntax);

    protected override string getDeclarationIdentifier(PropertyDeclarationSyntax syntax) => syntax.Identifier.Text;

    protected override string getImplementation(PropertyDeclarationSyntax syntax, string lensSingletonName)
    {
        var implementation = syntax
            .WithAccessorList(null) // remove { get; }
            .WithExpressionBody(
                SyntaxFactory.ArrowExpressionClause(
                    SyntaxFactory.ParseExpression("LensImplementations." + lensSingletonName)))
            // for some reason necessary for properties but not for methods
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)) 
            .WithAttributeLists(SyntaxFactory.List<AttributeListSyntax>());

        if (!implementation.Modifiers.Any(SyntaxKind.PartialKeyword))
            implementation = implementation.AddModifiers(SyntaxFactory.Token(SyntaxKind.PartialKeyword));

        return implementation.ToString();
    }
}