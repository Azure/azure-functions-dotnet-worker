// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using System.Collections.Immutable;
using System.Threading.Tasks;
using System.Composition;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Simplification;
using System.Threading;
using System.Linq;

namespace Microsoft.Azure.Functions.Worker.Sdk.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AsyncVoidCodeFixProvider)), Shared]
    public sealed class AsyncVoidCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticDescriptors.AsyncVoidReturnType.Id);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            Diagnostic diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(new VoidToTaskCodeAction(context.Document, diagnostic), diagnostic);
            
            return Task.CompletedTask;
        }

        /// <summary>
        /// CodeAction implementation which fixes changes async void to async Task as the return type of the method.
        /// </summary>
        private sealed class VoidToTaskCodeAction : CodeAction
        {
            private readonly Document _document;
            private readonly Diagnostic _diagnostic;

            internal VoidToTaskCodeAction(Document document, Diagnostic diagnostic)
            {
                this._document = document;
                this._diagnostic = diagnostic;
            }

            public override string Title => "Change return type of method to Task";

            /// null value is fine since we do not have more than one fix action from this code fix provider.
            public override string EquivalenceKey => null;

            /// <summary>
            /// Returns an updated Document where the async method return type is changed from void to Task.
            /// </summary>
            protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                SyntaxNode root = await _document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                
                MethodDeclarationSyntax methodDeclaration = root.FindNode(this._diagnostic.Location.SourceSpan)
                                                                .FirstAncestorOrSelf<MethodDeclarationSyntax>();

                TypeSyntax taskType = SyntaxFactory.ParseTypeName(Constants.Types.TaskType)
                                                   .WithAdditionalAnnotations(Simplifier.Annotation)
                                                   .WithTrailingTrivia(methodDeclaration.ReturnType.GetTrailingTrivia());
                
                MethodDeclarationSyntax newMethodDeclaration = methodDeclaration.WithReturnType(taskType);
                
                SyntaxNode newRoot = root.ReplaceNode(methodDeclaration, newMethodDeclaration);
                
                return _document.WithSyntaxRoot(newRoot);
            }
        }
    }
}
