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
            var methodSymbol = context.SemanticModel.GetDeclaredSymbol(context.Node);
            if (methodSymbol.IsVirtual || methodSymbol.IsOverride || methodSymbol.IsStatic)
                return;
            var visitor = new InstanceMemberDependenciesVisitor(context.SemanticModel, methodSymbol.ContainingType);
            var methodSyntax = context.Node as MethodDeclarationSyntax;
            methodSyntax.Accept(visitor);
            if (!visitor.MemberReferences.Any())
            {
                var diagnostic = Diagnostic.Create(Rule, methodSyntax.Identifier.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    internal class InstanceMemberDependenciesVisitor : CSharpSyntaxWalker
    {
        private SemanticModel _semModel;
        private INamedTypeSymbol _containingType;
        private List<SyntaxNode> _memberReferences = new List<SyntaxNode>();

        public InstanceMemberDependenciesVisitor(SemanticModel semanticModel, INamedTypeSymbol containingType)
        {
            _semModel = semanticModel;
            _containingType = containingType;
        }

        public IEnumerable<SyntaxNode> MemberReferences => _memberReferences;

        private bool IsLeftMostNode(CSharpSyntaxNode node)
        {
            if (node.Parent is InvocationExpressionSyntax invocation)
            {
                // node is the 
                return IsLeftMostNode(invocation);
            }
            else if (node.Parent is MemberAccessExpressionSyntax memberAccess)
            {
                // return true when node is the member access lhs
                return memberAccess.Expression == node;
            }
            else if (node.Parent is MemberBindingExpressionSyntax memberBindingAccess)
            {
                // a member binding is always a rhs
                return false;
            }
            else if (node.Parent is ConditionalAccessExpressionSyntax conditionalAccess)
            {
                // return true when node is the conditional member access lhs
                return conditionalAccess.Expression == node;
            }
            else
            {
                // the identfier is not qualified => it is considered as left most.
                return true;
            }
        }

        public override void VisitThisExpression(ThisExpressionSyntax node)
        {
            _memberReferences.Add(node);
        }

        public override void VisitIdentifierName(IdentifierNameSyntax node)
        {
            var symbol = _semModel.GetSymbolInfo(node).Symbol;
            if (symbol== null)
            {
                _memberReferences.Add(node);
            }
            else if (!(symbol is ITypeSymbol) && !symbol.IsStatic)
            {
                var parentTypeSymbol = symbol.ContainingType;
                if (SymbolEqualityComparer.Default.Equals(parentTypeSymbol, _containingType) ||
                    GetBaseTypeHierarchy(_containingType).FirstOrDefault(_ => SymbolEqualityComparer.Default.Equals(parentTypeSymbol, _)) != null)
                {
                    if (IsLeftMostNode(node))
                    {
                        _memberReferences.Add(node);
                    }
                }
            }
            base.VisitIdentifierName(node);
        }

        private static IEnumerable<INamedTypeSymbol> GetBaseTypeHierarchy(INamedTypeSymbol type)
        {
            if (type.BaseType != null)
            {
                yield return type.BaseType;
                foreach (var baseType in GetBaseTypeHierarchy(type.BaseType))
                {
                    yield return baseType;
                }
            }
        }
    }
}