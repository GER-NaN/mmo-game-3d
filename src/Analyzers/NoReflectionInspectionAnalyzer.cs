namespace Analyzers;

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

// GAME0006. Severity is set in .editorconfig, like the StyleCop and IDE rules.
// Why: for performance, and to reduce hidden complexity.
//
// BannedSymbols.txt bans the reflection types; it cannot ban the Type methods that only
// ask questions, because that needs one line per overload. This asks the compiler.
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NoReflectionInspectionAnalyzer : DiagnosticAnalyzer
{
    public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        "GAME0006",
        "Reflection",
        "Reflection belongs in startup, not here: '{0}' asks the runtime about a type",
        "Convention",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    // Methods declared on System.Type. obj.GetType() is not one of them: that method is
    // declared on System.Object, and it stays allowed.
    private static readonly HashSet<string> InspectingMethods = new HashSet<string>
    {
        "GetConstructor",
        "GetConstructors",
        "GetEvent",
        "GetEvents",
        "GetField",
        "GetFields",
        "GetGenericArguments",
        "GetGenericTypeDefinition",
        "GetInterface",
        "GetInterfaces",
        "GetMember",
        "GetMembers",
        "GetMethod",
        "GetMethods",
        "GetNestedType",
        "GetNestedTypes",
        "GetProperties",
        "GetProperty",
        "GetType",
        "InvokeMember",
        "IsAssignableFrom",
        "IsAssignableTo",
        "IsInstanceOfType",
        "IsSubclassOf",
        "MakeArrayType",
        "MakeGenericType",
    };

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
    {
        get { return ImmutableArray.Create(Rule); }
    }

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(Report, SyntaxKind.InvocationExpression);
    }

    private static void Report(SyntaxNodeAnalysisContext context)
    {
        SymbolInfo symbolInfo = context.SemanticModel.GetSymbolInfo(context.Node);
        IMethodSymbol? method = symbolInfo.Symbol as IMethodSymbol;

        if (method == null)
        {
            return;
        }

        if (!InspectingMethods.Contains(method.Name))
        {
            return;
        }

        // The declaring type, so an overload list is never needed and a method of the
        // same name on another type is left alone.
        INamedTypeSymbol declaringType = method.ContainingType;

        if (declaringType == null || declaringType.Name != "Type")
        {
            return;
        }

        if (declaringType.ContainingNamespace == null || declaringType.ContainingNamespace.ToDisplayString() != "System")
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation(), method.Name));
    }
}
