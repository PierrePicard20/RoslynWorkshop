using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace Aneo.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DoNotLockThisAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "AE0001";

        private static readonly string Title = "Do not lock this";
        private static readonly string MessageFormat = "Do not lock this";
        private static readonly string Description = "locking this may lead to a deadlock";
        private const string Category = "Code quality";

        private static readonly DiagnosticDescriptor Rule = 
            new DiagnosticDescriptor( DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning
                                    , isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // Callbacks
            context.RegisterSyntaxNodeAction(AnalyzeThisToken, SyntaxKind.ThisExpression);
        }

        private static SyntaxKind[] _kinds = new SyntaxKind[] { SyntaxKind.LockStatement, SyntaxKind.InvocationExpression, SyntaxKind.Block };

        private static void AnalyzeThisToken(SyntaxNodeAnalysisContext context)
        {
            var node = context.Node.Ancestors().FirstOrDefault(_ => _kinds.Contains(_.Kind()));
            if (node != null)
            {
                switch (node.Kind())
                {
                    case SyntaxKind.LockStatement:
                        // case 'lock (this)'
                        var diagnostic = Diagnostic.Create(Rule, context.Node.GetLocation());
                        context.ReportDiagnostic(diagnostic);
                        break;
                    case SyntaxKind.InvocationExpression:
                        var symbol = context.SemanticModel.GetSymbolInfo(node);
                        var method = symbol.Symbol as IMethodSymbol;
                        var methodName = method?.MetadataName ?? string.Empty;
                        var className = method?.ReceiverType.MetadataName ?? string.Empty;
                        if ((methodName == "Enter" || methodName == "Exit") && className == "Monitor")
                        {
                            // case 'Monitor.Enter(this);' or 'Monitor.Exit(this);' 
                            diagnostic = Diagnostic.Create(Rule, context.Node.GetLocation());
                            context.ReportDiagnostic(diagnostic);
                        }
                        break;
                }
            }
        }
    }
}