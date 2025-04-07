// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.Azure.Functions.Worker.Sdk.Generators;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Microsoft.Azure.Functions.Sdk.Generator.Tests
{
    public class SymbolExtensionsTest
    {
        [Fact]
        public void TestIsOrDerivedFrom_WhenImplementationExists()
        {
            var sourceCode = @"
            internal class BaseAttribute
            {
            }

            internal class FooAttribute : BaseAttribute
            {
            }

            internal class FooAttributeTwo : FooAttribute
            {
            }";

            var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
            var compilation = CSharpCompilation.Create("MyCompilation", new[] { syntaxTree });
            var semanticModel = compilation.GetSemanticModel(syntaxTree);

            // Retrieve the symbol for the FooOutAttribute class
            var root = syntaxTree.GetRoot();
            var baseAttributeClassDeclaration = root.DescendantNodes().OfType<ClassDeclarationSyntax>().First(cd => cd.Identifier.Text == "BaseAttribute");
            var fooClassDeclaration = root.DescendantNodes().OfType<ClassDeclarationSyntax>().First(cd => cd.Identifier.Text == "FooAttribute");
            var fooTwoClassDeclaration = root.DescendantNodes().OfType<ClassDeclarationSyntax>().First(cd => cd.Identifier.Text == "FooAttributeTwo");
            var baseAttributeSymbol = semanticModel.GetDeclaredSymbol(baseAttributeClassDeclaration);
            var fooSymbol = semanticModel.GetDeclaredSymbol(fooClassDeclaration);
            var fooTwoSymbol = semanticModel.GetDeclaredSymbol(fooTwoClassDeclaration);

            Assert.NotNull(baseAttributeSymbol);
            Assert.NotNull(fooSymbol);
            Assert.NotNull(fooTwoSymbol);

            Assert.True(fooSymbol.IsOrDerivedFrom(baseAttributeSymbol));
            Assert.True(fooTwoSymbol.IsOrDerivedFrom(baseAttributeSymbol));
            Assert.True(fooTwoSymbol.IsOrDerivedFrom(fooSymbol));
        }

        [Fact]
        public void TestIsOrDerivedFrom_WhenImplementationDoesNotExist()
        {
            var sourceCode = @"
            internal class BaseAttribute
            {
            }

            internal class FooAttribute
            {
            }

            internal class OtherBaseAttribute
            {
            }

            internal class OtherFooAttribute : OtherBaseAttribute
            {
            }";

            var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
            var compilation = CSharpCompilation.Create("MyCompilation", new[] { syntaxTree });
            var semanticModel = compilation.GetSemanticModel(syntaxTree);

            // Retrieve the symbol for the FooOutAttribute class
            var root = syntaxTree.GetRoot();
            var baseAttributeClassDeclaration = root.DescendantNodes().OfType<ClassDeclarationSyntax>().First(cd => cd.Identifier.Text == "BaseAttribute");
            var fooClassDeclaration = root.DescendantNodes().OfType<ClassDeclarationSyntax>().First(cd => cd.Identifier.Text == "FooAttribute");
            var otherBaseAttributeClassDeclaration = root.DescendantNodes().OfType<ClassDeclarationSyntax>().First(cd => cd.Identifier.Text == "OtherBaseAttribute");
            var otherFooClassDeclaration = root.DescendantNodes().OfType<ClassDeclarationSyntax>().First(cd => cd.Identifier.Text == "OtherFooAttribute");
            var baseAttributeSymbol = semanticModel.GetDeclaredSymbol(baseAttributeClassDeclaration);
            var fooSymbol = semanticModel.GetDeclaredSymbol(fooClassDeclaration);
            var otherBaseAttributeSymbol = semanticModel.GetDeclaredSymbol(otherBaseAttributeClassDeclaration);
            var otherFooSymbol = semanticModel.GetDeclaredSymbol(otherFooClassDeclaration);

            Assert.NotNull(baseAttributeSymbol);
            Assert.NotNull(fooSymbol);
            Assert.NotNull(otherBaseAttributeSymbol);
            Assert.NotNull(otherFooSymbol);

            Assert.False(fooSymbol.IsOrDerivedFrom(baseAttributeSymbol));
            Assert.False(otherFooSymbol.IsOrDerivedFrom(baseAttributeSymbol));
        }
    }
}
