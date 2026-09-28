namespace Analyzers;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

// GAME0002. Severity is set in .editorconfig, like the StyleCop and IDE rules.
// Why: for clarity, the constructor sits with its fields.
// Records keep their parameter list: it is the record itself.
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NoPrimaryConstructorAnalyzer : DiagnosticAnalyzer
{
    public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        "GAME0002",
        "Primary constructor",
        "Write a constructor in the body, not a primary constructor",
        "Convention",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
    {
        get { return ImmutableArray.Create(Rule); }
    }

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(Report, SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration);
    }

    private static void Report(SyntaxNodeAnalysisContext context)
    {
        // Both kinds are type declarations, and a parameter list on one of them is a
        // primary constructor. A type without one leaves the list null.
        TypeDeclarationSyntax declaration = (TypeDeclarationSyntax)context.Node;

        if (declaration.ParameterList == null)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rule, declaration.ParameterList.GetLocation()));
    }
}
