namespace Analyzers;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

// GAME0003. Severity is set in .editorconfig, like the StyleCop and IDE rules.
// Why: isolation of threaded logic. The tick loop owns it.
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NoAsyncAwaitAnalyzer : DiagnosticAnalyzer
{
    public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        "GAME0003",
        "async or await",
        "Use the tick loop and non-blocking calls, not async or await",
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

        // The await keyword, for its own sake: a file with top-level statements can hold
        // one without any declaration to carry the async modifier.
        context.RegisterSyntaxNodeAction(ReportAwait, SyntaxKind.AwaitExpression);

        // Everything that can carry the async modifier.
        context.RegisterSyntaxNodeAction(
            ReportAsyncModifier,
            SyntaxKind.MethodDeclaration,
            SyntaxKind.LocalFunctionStatement,
            SyntaxKind.SimpleLambdaExpression,
            SyntaxKind.ParenthesizedLambdaExpression,
            SyntaxKind.AnonymousMethodExpression);
    }

    private static void ReportAwait(SyntaxNodeAnalysisContext context)
    {
        AwaitExpressionSyntax node = (AwaitExpressionSyntax)context.Node;
        context.ReportDiagnostic(Diagnostic.Create(Rule, node.AwaitKeyword.GetLocation()));
    }

    private static void ReportAsyncModifier(SyntaxNodeAnalysisContext context)
    {
        MethodDeclarationSyntax? method = context.Node as MethodDeclarationSyntax;

        if (method != null)
        {
            ReportIfAsync(context, method.Modifiers);
            return;
        }

        LocalFunctionStatementSyntax? localFunction = context.Node as LocalFunctionStatementSyntax;

        if (localFunction != null)
        {
            ReportIfAsync(context, localFunction.Modifiers);
            return;
        }

        // Lambdas and anonymous methods keep the keyword in a token of its own rather
        // than in a modifier list.
        AnonymousFunctionExpressionSyntax? anonymousFunction = context.Node as AnonymousFunctionExpressionSyntax;

        if (anonymousFunction != null && anonymousFunction.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, anonymousFunction.AsyncKeyword.GetLocation()));
        }
    }

    private static void ReportIfAsync(SyntaxNodeAnalysisContext context, SyntaxTokenList modifiers)
    {
        foreach (SyntaxToken modifier in modifiers)
        {
            if (modifier.IsKind(SyntaxKind.AsyncKeyword))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, modifier.GetLocation()));
            }
        }
    }
}
