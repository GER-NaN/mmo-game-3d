namespace Analyzers;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

// GAME0005. The rule reports every lock; .editorconfig says where that matters.
// Why: isolation of threaded logic. One thread drives the simulation.
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NoLockAnalyzer : DiagnosticAnalyzer
{
    public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        "GAME0005",
        "Lock statement",
        "This code is driven by one thread, so a lock means a design change: discuss it first",
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
        context.RegisterSyntaxNodeAction(Report, SyntaxKind.LockStatement);
    }

    private static void Report(SyntaxNodeAnalysisContext context)
    {
        LockStatementSyntax statement = (LockStatementSyntax)context.Node;
        context.ReportDiagnostic(Diagnostic.Create(Rule, statement.LockKeyword.GetLocation()));
    }
}
