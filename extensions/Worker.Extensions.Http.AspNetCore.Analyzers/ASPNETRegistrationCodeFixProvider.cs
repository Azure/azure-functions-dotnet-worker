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
using System.Linq.Expressions;

namespace Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ASPNETRegistrationCodeFixProvider)), Shared]
    public sealed class ASPNETRegistrationCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticDescriptors.CorrectRegistrationExpectedInAspNetIntegration.Id);

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
                var syntaxTree = await _document.GetSyntaxTreeAsync();
                var lineSpan = this._diagnostic.Location.SourceSpan;

                var node = syntaxTree.GetRoot().DescendantNodes(lineSpan)
                                   .First(n => lineSpan.Contains(n.FullSpan)).DescendantNodes()
                                   .OfType<IdentifierNameSyntax>().FirstOrDefault(c => c.Identifier.Text == "ConfigureFunctionsWorkerDefaults");

                var newNode = node.ReplaceNode(node, SyntaxFactory.IdentifierName("ConfigureFunctionsWebApplication"));
                var newSyntaxRoot = syntaxTree.GetRoot().ReplaceNode(node, newNode);
                return _document.WithSyntaxRoot(newSyntaxRoot);
            }
        }
    }
}
