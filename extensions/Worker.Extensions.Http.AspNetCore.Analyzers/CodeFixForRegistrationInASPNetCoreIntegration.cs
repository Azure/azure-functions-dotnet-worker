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
using System.Threading;
using System.Linq;

namespace Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CodeFixForRegistrationInASPNetCoreIntegration)), Shared]
    public sealed class CodeFixForRegistrationInASPNetCoreIntegration : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticDescriptors.CorrectRegistrationExpectedInAspNetIntegration.Id);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        private const string ExpectedRegistrationMethod = "ConfigureFunctionsWebApplication";

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            Diagnostic diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(new ChangeConfigurationForASPNetIntegration(context.Document, diagnostic), diagnostic);

            return Task.CompletedTask;
        }

        /// <summary>
        /// CodeAction implementation which fixes the method configuration for ASP.NET Core Integration.
        /// </summary>
        private sealed class ChangeConfigurationForASPNetIntegration : CodeAction
        {
            private readonly Document _document;
            private readonly Diagnostic _diagnostic;

            internal ChangeConfigurationForASPNetIntegration(Document document, Diagnostic diagnostic)
            {
                this._document = document;
                this._diagnostic = diagnostic;
            }

            public override string Title => "Change configuration for ASP.NET Core Integration";

            public override string EquivalenceKey => null;

            /// <summary>
            /// Returns an updated Document with correct method configuration for ASP.NET Core Integration.
            /// </summary>
            protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                SyntaxNode root = await _document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                var currentNode = root.FindNode(this._diagnostic.Location.SourceSpan).FirstAncestorOrSelf<IdentifierNameSyntax>();

                var newNode = currentNode.ReplaceNode(currentNode, SyntaxFactory.IdentifierName(ExpectedRegistrationMethod));

                SyntaxNode newSyntaxRoot = root.ReplaceNode(currentNode, newNode);

                return _document.WithSyntaxRoot(newSyntaxRoot);
            }
        }
    }
}
