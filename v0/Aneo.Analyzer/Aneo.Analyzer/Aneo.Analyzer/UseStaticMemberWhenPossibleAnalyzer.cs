using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace Aneo.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UseStaticMemberWhenPossibleAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "AE0002";

        private static readonly string Title = "Use a static member when possible";
        private static readonly string MessageFormat = "Use a static member when possible";
        private static readonly string Description = "Use a static member when possible";
        private const string Category = "Code quality";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
        }

        private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
        {
            // TO BE COMPLETED
        }
    }
}