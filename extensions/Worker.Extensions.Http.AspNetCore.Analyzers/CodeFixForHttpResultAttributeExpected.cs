// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

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
            context.RegisterCodeFix(new AddHttpResultAttribute(context.Document, diagnostic), diagnostic);

            return Task.CompletedTask;
        }

        /// <summary>
        /// CodeAction implementation which adds the HttpResultAttribute on the return type of a function using the multi-output bindings pattern.
        /// </summary>
        private sealed class AddHttpResultAttribute : CodeAction
        {
            private readonly Document _document;
            private readonly Diagnostic _diagnostic;
            private readonly string ExpectedAttributeName = "HttpResultAttribute";


            internal AddHttpResultAttribute(Document document, Diagnostic diagnostic)
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
                var semanticModel = await _document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                var typeNode = root.FindNode(this._diagnostic.Location.SourceSpan)
                    .FirstAncestorOrSelf<TypeSyntax>();

                var typeSymbol = semanticModel.GetSymbolInfo(typeNode).Symbol;
                var typeDeclarationSyntaxReference = typeSymbol.DeclaringSyntaxReferences.FirstOrDefault();
                if (typeDeclarationSyntaxReference == null)
                {
                    return _document;
                }

                var typeDeclarationNode = await typeDeclarationSyntaxReference.GetSyntaxAsync(cancellationToken);

                var propertyNode = typeDeclarationNode.DescendantNodes()
                    .OfType<PropertyDeclarationSyntax>()
                    .First(prop =>
                    {
                        var propertyType = semanticModel.GetTypeInfo(prop.Type).Type;
                        return propertyType != null && propertyType.Name == "IActionResult";
                    });

                var attribute = SyntaxFactory.Attribute(SyntaxFactory.IdentifierName(ExpectedAttributeName));

                var newPropertyNode = propertyNode
                    .AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(attribute)));

                var newRoot = root.ReplaceNode(propertyNode, newPropertyNode);

                return _document.WithSyntaxRoot(newRoot);
            }
        }
    }
}
