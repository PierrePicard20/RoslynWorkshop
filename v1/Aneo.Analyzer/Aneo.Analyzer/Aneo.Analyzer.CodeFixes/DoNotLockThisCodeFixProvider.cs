using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;

namespace Aneo.Analyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DoNotLockThisCodeFixProvider)), Shared]
    public class DoNotLockThisCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(DoNotLockThisAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // fetch the node breaking the rule
            var thisNode = root.FindNode(diagnosticSpan);

            // fetch the object fields already existing
            var semanticModel = await context.Document.GetSemanticModelAsync().ConfigureAwait(false);
            var typeDecl = GetContainingTypeDeclaration(thisNode);
            if (typeDecl == null)
            {
                return;
            }
            var fields = GetPrivateObjectFieldMembers(typeDecl, semanticModel);
            var field = fields.FirstOrDefault(_ => _.Name == _defaultLockerName);

            // field == null => the fix add the default locker
            // field != null => the fix the already existing default locker
            context.RegisterCodeFix(CodeAction.Create(title: CodeFixResources.CodeFixTitle
                                                        , createChangedDocument: ct => FixLock(context.Document, thisNode, typeDecl, field, semanticModel, root)
                                                        )
                                    , diagnostic);
        }

        private const string _defaultLockerName = "_locker";

        private static IEnumerable<MemberDeclarationSyntax> ConcatMembers(MemberDeclarationSyntax member, SyntaxList<MemberDeclarationSyntax> members)
        {
            yield return member;
            foreach (var memberDeclaration in members)
            {
                yield return memberDeclaration;
            }
        }

        private TypeDeclarationSyntax GetContainingTypeDeclaration(SyntaxNode thisNode)
        {
            return thisNode.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();
        }

        private IEnumerable<IFieldSymbol> GetPrivateObjectFieldMembers(TypeDeclarationSyntax typeDecl, SemanticModel semanticModel)
        {
            var typeSymbol = semanticModel.GetDeclaredSymbol(typeDecl);
            return typeSymbol.GetMembers().OfType<IFieldSymbol>().Where(_ => _.Type.SpecialType == SpecialType.System_Object 
                                                                          && _.DeclaredAccessibility==Accessibility.Private
                                                                          && !_.IsStatic);
        }

        private Task<Document> FixLock( Document document
                                      , SyntaxNode thisNode
                                      , TypeDeclarationSyntax typeDecl
                                      , IFieldSymbol lockerField
                                      , SemanticModel semanticModel
                                      , SyntaxNode root)
        {
            return Task.Run(() =>
            {
                // Setting up the node list to replace
                var nodesToReplace = new List<SyntaxNode> { thisNode };
                if (lockerField == null)
                {
                    // no locker symbol specified
                    // => the type node has to be replaced to add a field declaration for a '_locker'.
                    nodesToReplace.Add(typeDecl);
                }

                // Let's replace the nodes!
                // ReplacesNodes rebuilds the AST from the bottom up.
                // Lambda parameter oldNode represent the old sub tree without any sub nodes replaced.
                // Lambda parameter newNode represent the new sub tree.
                var newRoot = root.ReplaceNodes(nodesToReplace, (oldNode, newNode) =>
                {
                    if (newNode.Kind() == typeDecl.Kind())
                    {
                        // add a the default locker field declaration as new type member.
                        typeDecl = newNode as TypeDeclarationSyntax;
                        var indentTrivia = typeDecl.GetLeadingTrivia().Where(_ => _.IsKind(SyntaxKind.WhitespaceTrivia)).ToSyntaxTriviaList();
                        indentTrivia = indentTrivia.Add(SyntaxFactory.Whitespace("    "));
                        var lockerDecl = SyntaxFactory.ParseMemberDeclaration($"private object {_defaultLockerName} = new object();" + Environment.NewLine)
                                                      .WithLeadingTrivia(indentTrivia);
                        var list = new SyntaxList<MemberDeclarationSyntax>(ConcatMembers(lockerDecl, typeDecl.Members));
                        return typeDecl.WithMembers(list);
                    }
                    else if (newNode == thisNode)
                    {
                        var newLockIdent = SyntaxFactory.ParseExpression(lockerField?.Name ?? _defaultLockerName);
                        if (thisNode.Kind() == SyntaxKind.Argument)
                        {
                            // replace the argment of 'Enter' and 'Exit'
                            return SyntaxFactory.Argument(newLockIdent);
                        }
                        else
                        {
                            // replace the expression in 'lock (this)'
                            return newLockIdent;
                        }
                    }
                    return null;
                });

                return document.WithSyntaxRoot(newRoot);
            });
        }
    }
}
