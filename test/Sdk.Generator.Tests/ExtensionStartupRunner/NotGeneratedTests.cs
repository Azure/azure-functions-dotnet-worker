// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.Azure.Functions.Sdk.Generator.Tests;
using Microsoft.Azure.Functions.Tests.WorkerExtensionsSample;
using Microsoft.Azure.Functions.Worker.Sdk.Generators;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Microsoft.Azure.Functions.Sdk.Generator.ExtensionStartupRunner.Tests
{
    public partial class ExtensionStartupRunnerGeneratorTests
    {
        public sealed class NotGeneratedTests
        {
            const string InputCode = """
                                 public class Foo
                                 {
                                 }
                                 """;

            [Theory]
            [InlineData(LanguageVersion.CSharp7_3)]
            [InlineData(LanguageVersion.CSharp8)]
            [InlineData(LanguageVersion.CSharp9)]
            [InlineData(LanguageVersion.CSharp10)]
            [InlineData(LanguageVersion.CSharp11)]
            [InlineData(LanguageVersion.Latest)]
            public async Task NotGeneratedWhenNotRunningInAnAzureFunctionsProject(LanguageVersion languageVersion)
            {
                var referencedExtensionAssemblies = new[]
                {
                    typeof(SampleExtensionStartup).Assembly,
                };

                string? expectedGeneratedFileName = null;
                string? expectedOutput = null;

                await TestHelpers.RunTestAsync<ExtensionStartupRunnerGenerator>(
                    referencedExtensionAssemblies,
                    InputCode,
                    expectedGeneratedFileName,
                    expectedOutput,
                    languageVersion: languageVersion,
                    runInsideAzureFunctionProject: false);
            }
        }
    }
}
