// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CodeFixForHttpResultAttribute)), Shared]
    public sealed class CodeFixForHttpResultAttribute : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticDescriptors.MultipleOutputHttpTriggerWithoutHttpResultAttribute.Id);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

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
            private readonly string ExpectedAttributeName = "HttpResultAttribute";


            internal ChangeConfigurationForASPNetIntegration(Document document, Diagnostic diagnostic)
            {
                this._document = document;
                this._diagnostic = diagnostic;
            }

            public override string Title => "Add HttpResultAttribute";

            public override string EquivalenceKey => null;

            /// <summary>
            /// Returns an updated Document with HttpResultAttribute added to the relevant property.
            /// </summary>
            protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                // Get the syntax root of the document
                var root = await _document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                // Find the property that needs the attribute
                var propertyNode = root.DescendantNodes()
                    .OfType<PropertyDeclarationSyntax>()
                    .First(); // Adjust this to find the correct property

                var attribute = SyntaxFactory.Attribute(SyntaxFactory.IdentifierName(ExpectedAttributeName));

                // Add the attribute to the property
                var newPropertyNode = propertyNode.AddAttributeLists(
                    SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(attribute)));

                // Replace the old property with the new property in the syntax root
                var newRoot = root.ReplaceNode(propertyNode, newPropertyNode);

                return _document.WithSyntaxRoot(newRoot);
            }
        }
    }
}
