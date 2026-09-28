namespace Analyzers;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

// GAME0004. Severity is set in .editorconfig, like the StyleCop and IDE rules.
// Why: for clarity, each name shows its own type.
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NoTupleDeconstructionAnalyzer : DiagnosticAnalyzer
{
    public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        "GAME0004",
        "Tuple deconstruction",
        "Declare each variable on its own line, not by deconstructing a tuple",
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

        // var (a, b) = ..., (int a, int b) = ... and (a, b) = ... are all assignments
        // whose left side is a tuple or a grouped declaration.
        context.RegisterSyntaxNodeAction(ReportAssignment, SyntaxKind.SimpleAssignmentExpression);

        // foreach ((a, b) in pairs) is a statement of its own kind.
        context.RegisterSyntaxNodeAction(ReportForEach, SyntaxKind.ForEachVariableStatement);
    }

    private static void ReportAssignment(SyntaxNodeAnalysisContext context)
    {
        AssignmentExpressionSyntax assignment = (AssignmentExpressionSyntax)context.Node;

        // (a, b) = pair, with the variables declared earlier.
        TupleExpressionSyntax? tuple = assignment.Left as TupleExpressionSyntax;

        if (tuple != null)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, tuple.GetLocation()));
            return;
        }

        // var (a, b) = pair, where the left side declares the variables in one group.
        DeclarationExpressionSyntax? declaration = assignment.Left as DeclarationExpressionSyntax;

        if (declaration != null && declaration.Designation is ParenthesizedVariableDesignationSyntax)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, declaration.GetLocation()));
        }
    }

    private static void ReportForEach(SyntaxNodeAnalysisContext context)
    {
        ForEachVariableStatementSyntax statement = (ForEachVariableStatementSyntax)context.Node;
        context.ReportDiagnostic(Diagnostic.Create(Rule, statement.Variable.GetLocation()));
    }
}
