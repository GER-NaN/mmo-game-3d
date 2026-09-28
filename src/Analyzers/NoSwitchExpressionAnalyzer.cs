namespace Analyzers;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

// GAME0001. Severity is set in .editorconfig, like the StyleCop and IDE rules.
// Why: by convention, for plain syntax and clarity.
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NoSwitchExpressionAnalyzer : DiagnosticAnalyzer
{
    public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        "GAME0001",
        "Switch expression",
        "Use a switch statement, not a switch expression",
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
        context.RegisterSyntaxNodeAction(Report, SyntaxKind.SwitchExpression);
    }

    private static void Report(SyntaxNodeAnalysisContext context)
    {
        SwitchExpressionSyntax node = (SwitchExpressionSyntax)context.Node;
        context.ReportDiagnostic(Diagnostic.Create(Rule, node.SwitchKeyword.GetLocation()));
    }
}
