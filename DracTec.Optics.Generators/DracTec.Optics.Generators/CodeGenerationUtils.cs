using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DracTec.Optics.Generators;

public static class CodeGenerationUtils {

    /// Sets up things like the namespace declaration and containing class(es). Works for top-level
    /// type declarations, type declarations in namespaces, inner type declarations and type members.
    public static (string prefix, int indentation, string postfix) MkContext(
        SyntaxNode? part, 
        IEnumerable<string> defaultIncludes, 
        bool includeSelf = false, 
        bool includeOpenBrace = true
    ) {
        var openBrace = includeOpenBrace ? " {\n" : "";
        var context = includeSelf ? part : part?.Parent;
        defaultIncludes = defaultIncludes as IReadOnlyCollection<string> ?? defaultIncludes.ToList();
        return context switch {
            null => ("", 0, ""),
            CompilationUnitSyntax cus => handleCompilationUnitSyntax(cus),
            NamespaceDeclarationSyntax nds when 
                MkContext(includeSelf ? nds.Parent : nds, defaultIncludes, includeSelf) is {prefix: var pre, indentation: var i, postfix: var post}
                    => ($"{pre}namespace {nds.Name} {{\n{(nds.Usings.Count > 0 ? $"{Indent(i + 1)}{nds.Usings}\n" : "")}\n", i + 1, $"}}{post}"),
            TypeDeclarationSyntax tds when 
                MkContext(includeSelf ? tds.Parent : tds, defaultIncludes, includeSelf) is {prefix: var pre, indentation: var i, postfix: var post}
                    => ($"{pre}{Indent(i)}{tds.Modifiers} {tds.Keyword} {classOrStructKeywordFor(tds)} {tds.Identifier}{GenericsFor(tds)}{openBrace}", i + 1, $"{Indent(i)}}}\n{post}"),
            _ => MkContext(context.Parent, defaultIncludes, includeSelf, includeOpenBrace)
        };

        (string prefix, int indentation, string postfix) handleCompilationUnitSyntax(CompilationUnitSyntax cus)
        {
            var fileScopedNamespace = cus.Members.OfType<FileScopedNamespaceDeclarationSyntax>().FirstOrDefault();
            if (fileScopedNamespace != null)
                return ($"{string.Join("\n", defaultIncludes.Select(inc => $"using {inc};").Distinct())}\n\nnamespace {fileScopedNamespace.Name};\n\n", 0, "");
            else return ($"{string.Join("\n", cus.Usings.Select(u => u.ToString()).Concat(defaultIncludes.Select(inc => $"using {inc};")).Distinct())}\n\n", 0, "");
        }
        
        string classOrStructKeywordFor(TypeDeclarationSyntax tds) => tds switch {
            RecordDeclarationSyntax rds => rds.ClassOrStructKeyword.ToString(),
            _ => ""
        };
    }

    public static string Indent(int depth) => string.Join("", Enumerable.Repeat("    ", depth));

    public static string GenericsFor(TypeDeclarationSyntax tds) =>
        tds.TypeParameterList is { } tps ? tps.ToString() : "";
    
    public static string UniqueFileNameFor(INamedTypeSymbol symbol) => 
        // Use the fully qualified metadata name and replace invalid characters for file names
        symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            .Replace("<", ".")
            .Replace(">", ".")
            .Replace(",", ".")
            .Replace("[", ".")
            .Replace("]", ".")
            .Replace("::", ".");
}