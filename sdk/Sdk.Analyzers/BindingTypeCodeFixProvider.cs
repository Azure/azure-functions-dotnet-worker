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
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CodeRefactorings;

namespace Microsoft.Azure.Functions.Worker.Sdk.Analyzers
{

    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(BindingTypeCodeFixProvider)), Shared]
    public sealed class BindingTypeCodeFixProvider : CodeRefactoringProvider
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            // Diagnostic diagnostic = context.Diagnostics.First();
            // TextSpan diagnosticSpan = diagnostic.Location.SourceSpan;

            SyntaxNode root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            // MethodDeclarationSyntax methodDeclaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().First();

            var parameters = root.DescendantNodes().OfType<ParameterSyntax>();

            // var parameters = methodDeclaration.ParameterList.Parameters;

            foreach (var parameter in parameters)
            {
                await AnalyzeForCodeFix(context, parameter);
            }
        }

        private async Task AnalyzeForCodeFix(CodeRefactoringContext context, ParameterSyntax parameter)
        {
            var semanticModel = await context.Document.GetSemanticModelAsync().ConfigureAwait(false);
            var parameterSymbol = semanticModel.GetDeclaredSymbol(parameter);

            foreach (var attribute in parameterSymbol.GetAttributes())
            {
                var attributeType = attribute?.AttributeClass;
                var inputConverterAttributes = GetInputConverterAttributes(semanticModel, attributeType);

                if (inputConverterAttributes.Count <= 0)
                {
                    continue;
                }

                var supportedTypes = GetSupportedTypes(semanticModel, inputConverterAttributes);

                if (supportedTypes.Count <= 0)
                {
                    continue;
                }

                foreach (ITypeSymbol supportedType in supportedTypes)
                {
                    string name = supportedType.ToMinimalDisplayString(semanticModel, 0);

                    if (name.Contains("IEnumerable"))
                    {
                        name = Regex.Match(name, @"IEnumerable<[^>]+>").Value;
                    }

                    // Create a code action for each potential supported type
                    context.RegisterRefactoring(
                        new SupportedBindingTypeCodeAction(context.Document, parameter, name));
                }
            }
        }

        private static List<AttributeData> GetInputConverterAttributes(SemanticModel model, ITypeSymbol attributeType)
        {
            var inputConverterAttributeType = model.Compilation.GetTypeByMetadataName(Constants.Types.InputConverterAttribute);
            return attributeType.GetAttributes()
                .Where(attr => attr.AttributeClass.Equals(inputConverterAttributeType))
                .ToList();
        }

        private static List<ITypeSymbol> GetSupportedTypes(SemanticModel model, List<AttributeData> inputConverterAttributes)
        {
            var supportedTypes = new List<ITypeSymbol>();

            foreach (var inputConverterAttribute in inputConverterAttributes)
            {
                var converterName = inputConverterAttribute.ConstructorArguments.FirstOrDefault().Value.ToString();
                var converter = model.Compilation.GetTypeByMetadataName(converterName);
                var converterAttributes = converter.GetAttributes();

                supportedTypes.AddRange(converterAttributes
                    .Where(a => a.AttributeClass.Name == Constants.Names.SupportedConverterTypeAttribute)
                    .Select(a => (ITypeSymbol) a.ConstructorArguments.FirstOrDefault().Value)
                    .ToList());
            }

            return supportedTypes;
        }

        /// <summary>
        /// CodeAction implementation which fixes changes async void to async Task as the return type of the method.
        /// </summary>
        private sealed class SupportedBindingTypeCodeAction : CodeAction
        {
            private readonly Document _document;
            private readonly ParameterSyntax _parameterSyntax;
            private readonly string _supportedType;

            internal SupportedBindingTypeCodeAction(Document document, ParameterSyntax parameterSyntax, string supportedType)
            {
                this._document = document;
                this._parameterSyntax = parameterSyntax;
                this._supportedType = supportedType;
            }

            public override string Title => $"Bind to {_supportedType}";

            /// null value is fine since we do not have more than one fix action from this code fix provider.
            public override string EquivalenceKey => null;

            /// <summary>
            /// Returns an updated Document where the invalid binding type is replaced with a supported type.
            /// </summary>
            protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                SyntaxTrivia spaceTrivia = SyntaxFactory.Whitespace(" ");

                TypeSyntax newTypeSyntax = SyntaxFactory.ParseTypeName(_supportedType);
                ParameterSyntax newParameterSyntax = _parameterSyntax
                    .WithType(newTypeSyntax)
                    .WithIdentifier(_parameterSyntax.Identifier.WithLeadingTrivia(spaceTrivia));

                SyntaxNode root = await _document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                SyntaxNode newRoot = root.ReplaceNode(_parameterSyntax, newParameterSyntax);

                return _document.WithSyntaxRoot(newRoot);
            }
        }
    }
}
