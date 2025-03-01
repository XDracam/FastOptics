using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DracTec.Optics.Generators;

// A roslyn source analyzer that validates all instances of [Lens(".Foo.Bar")]
// Reports an error diagnostic when the return type of the method or property with the attribute is not
//  an ILens<A, B> for some types A and B.
// Also reports an error diagnostic if given some `a` of type `A`
//  the expression `a.Foo.Bar` is not an expression of type B (generalized for whatever path is specified in the attribute).

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class LensAnalyzer : DiagnosticAnalyzer
{
    public static class ReturnTypeDiagnostic
    {
        public const string DiagnosticId = "LensAttributeReturnType";
        private const string Title = "Invalid [Lens(\"...\")] declaration";
        private const string MessageFormat = "The method or property with the [Lens(\"...\")] attribute must return an ILens<A, B>";
        private const string Description = "The method or property with the [Lens(\"...\")] attribute must return an ILens<A, B>.";
        private const string Category = "DracTec.Optics";

        public static readonly DiagnosticDescriptor Rule = 
            new(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);
    }
    
    public static class ValidPathDiagnostic
    {
        public const string DiagnosticId = "LensAttributePath";
        private const string Title = "Invalid path in [Lens(path)] declaration";
        private const string MessageFormat = "'{0}' must be a valid expression on type '{1}' that returns '{2}' but {3}";
        private const string Description = "The path in [Lens(path)] must be a valid expression that conforms to the types in the returned ILens<A, B>.";
        private const string Category = "DracTec.Optics";

        public static readonly DiagnosticDescriptor Rule = 
            new(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);
    }

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => 
        [ReturnTypeDiagnostic.Rule, ValidPathDiagnostic.Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.MethodDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.PropertyDeclaration);
    }

    private void AnalyzeNode(SyntaxNodeAnalysisContext context)
    {
        var syntaxNode = context.Node;
        var semanticModel = context.SemanticModel;

        if (syntaxNode is not MethodDeclarationSyntax or PropertyDeclarationSyntax) return;

        AttributeSyntax? lensAttribute = null;
        INamedTypeSymbol? returnType = null;

        if (syntaxNode is MethodDeclarationSyntax methodDeclarationSyntax)
        {
            if (semanticModel.GetDeclaredSymbol(methodDeclarationSyntax)?.ReturnType is not INamedTypeSymbol rt) 
                return;
            returnType = rt;
            lensAttribute = methodDeclarationSyntax.AttributeLists.SelectMany(al => al.Attributes)
                .FirstOrDefault(a => a.Name.ToString() == "Lens");
        } 
        else if (syntaxNode is PropertyDeclarationSyntax propertyDeclarationSyntax)
        {
            if (semanticModel.GetDeclaredSymbol(propertyDeclarationSyntax)?.Type is not INamedTypeSymbol rt) 
                return;
            returnType = rt;
            lensAttribute = propertyDeclarationSyntax.AttributeLists.SelectMany(al => al.Attributes)
                .FirstOrDefault(a => a.Name.ToString() == "Lens");
        }
        
        if (lensAttribute == null) return;
        if (!returnType!.Name.StartsWith("ILens")) 
            context.ReportDiagnostic(Diagnostic.Create(ReturnTypeDiagnostic.Rule, lensAttribute.GetLocation()));

        if (returnType.TypeArguments.Length != 2) return;
        var recordType = returnType.TypeArguments[0];
        var valueType = returnType.TypeArguments[1];
        
        // Validate that given some `a` of type `A` the expression `a.Foo.Bar` is an expression
        //  of type B (generalized for whatever path is specified as argument of the attribute, e.g. `[Lens(".Foo.Bar")]`.

        var expression =
            lensAttribute.ArgumentList?.Arguments.SingleOrDefault()?.Expression as LiteralExpressionSyntax;
        var path = expression?.Token.ValueText;
        
        if (path == null) return;

        var pathExpressionType = semanticModel.GetSpeculativeTypeInfo(0,
            SyntaxFactory.ParseExpression($"default({recordType}){path}"), SpeculativeBindingOption.BindAsExpression).Type;
        
        if (!SymbolEqualityComparer.Default.Equals(valueType, pathExpressionType))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                ValidPathDiagnostic.Rule, 
                expression!.GetLocation(), 
                path,
                recordType.ToString(), 
                valueType.ToString(),
                pathExpressionType == null ? "the expression does not compile" : $"it has type '{pathExpressionType}'"
            ));
        }
    }
}