﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Tests.WorkerExtensionsSample;
using Microsoft.Azure.Functions.Worker.Sdk.Generators;
using Microsoft.CodeAnalysis.Testing;
using Worker.Extensions.Sample_IncorrectImplementation;
using Xunit;
namespace Microsoft.Azure.Functions.SdkGeneratorTests
{
    public class ExtensionStartupRunnerGeneratorTests
    {
        const string InputCode = """
                                 public class Foo
                                 {
                                 }
                                 """;

        [Fact]
        public async Task StartupExecutorCodeGetsGenerated()
        {
            // Source generation is based on referenced assembly.
            var referencedExtensionAssemblies = new[]
            {
                typeof(SampleExtensionStartup).Assembly,
            };

            string expectedGeneratedFileName = $"WorkerExtensionStartupCodeExecutor.g.cs";
            string expectedOutput = """
                                    // <auto-generated/>
                                    using System;
                                    using Microsoft.Azure.Functions.Worker;
                                    using Microsoft.Azure.Functions.Worker.Core;

                                    [assembly: WorkerExtensionStartupCodeExecutorInfo(typeof(TestProject.WorkerExtensionStartupCodeExecutor))]

                                    namespace TestProject
                                    {
                                        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
                                        internal class WorkerExtensionStartupCodeExecutor : WorkerExtensionStartup
                                        {
                                            /// <summary>
                                            /// Configures the worker to register extension startup services.
                                            /// </summary>
                                            /// <param name="applicationBuilder">The <see cref="IFunctionsWorkerApplicationBuilder"/> to configure.</param>
                                            public override void Configure(IFunctionsWorkerApplicationBuilder applicationBuilder)
                                            {
                                                try
                                                {
                                                    new Microsoft.Azure.Functions.Tests.WorkerExtensionsSample.SampleExtensionStartup().Configure(applicationBuilder);
                                                }
                                                catch (Exception ex)
                                                {
                                                    Console.Error.WriteLine("Error calling Configure on Microsoft.Azure.Functions.Tests.WorkerExtensionsSample.SampleExtensionStartup instance."+ex.ToString());
                                                }
                                            }
                                        }
                                    }
                                    """;

            await TestHelpers.RunTestAsync<ExtensionStartupRunnerGenerator>(
                referencedExtensionAssemblies,
                InputCode,
                expectedGeneratedFileName,
                expectedOutput);
        }

        [Fact]
        public async Task StartupExecutorCodeDoesNotGetsGeneratedWheNoExtensionAssembliesAreReferenced()
        {
            // source gen will happen only when an assembly with worker startup type is defined.
            var referencedExtensionAssemblies = Array.Empty<System.Reflection.Assembly>();

            string? expectedGeneratedFileName = null;
            string? expectedOutput = null;

            await TestHelpers.RunTestAsync<ExtensionStartupRunnerGenerator>(
                referencedExtensionAssemblies,
                InputCode,
                expectedGeneratedFileName,
                expectedOutput);
        }

        [Fact]
        public async Task DiagnosticErrorsAreReportedWhenStartupTypeIsInvalid()
        {
            var referencedExtensionAssemblies = new[]
            {
                // An assembly with valid extension startup implementation
                typeof(SampleExtensionStartup).Assembly,
                // and an assembly with invalid implementation
                typeof(SampleIncorrectExtensionStartup).Assembly,
            };

            // Our generator will create code for the good implementation
            // and report 2 diagnostic errors for the bad implementation.
            string expectedGeneratedFileName = $"WorkerExtensionStartupCodeExecutor.g.cs";
            string expectedOutput = """
                                    // <auto-generated/>
                                    using System;
                                    using Microsoft.Azure.Functions.Worker;
                                    using Microsoft.Azure.Functions.Worker.Core;

                                    [assembly: WorkerExtensionStartupCodeExecutorInfo(typeof(MyCompany.MyProject.MyApp.WorkerExtensionStartupCodeExecutor))]

                                    namespace MyCompany.MyProject.MyApp
                                    {
                                        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
                                        internal class WorkerExtensionStartupCodeExecutor : WorkerExtensionStartup
                                        {
                                            /// <summary>
                                            /// Configures the worker to register extension startup services.
                                            /// </summary>
                                            /// <param name="applicationBuilder">The <see cref="IFunctionsWorkerApplicationBuilder"/> to configure.</param>
                                            public override void Configure(IFunctionsWorkerApplicationBuilder applicationBuilder)
                                            {
                                                try
                                                {
                                                    new Microsoft.Azure.Functions.Tests.WorkerExtensionsSample.SampleExtensionStartup().Configure(applicationBuilder);
                                                }
                                                catch (Exception ex)
                                                {
                                                    Console.Error.WriteLine("Error calling Configure on Microsoft.Azure.Functions.Tests.WorkerExtensionsSample.SampleExtensionStartup instance."+ex.ToString());
                                                }
                                            }
                                        }
                                    }
                                    """;

            var expectedDiagnosticResults = new List<DiagnosticResult>
            {
                new DiagnosticResult(DiagnosticDescriptors.IncorrectBaseType)
                // these arguments are the value we pass as the message format parameters when creating the DiagnosticDescriptor instance.
                .WithArguments("Worker.Extensions.Sample_IncorrectImplementation.SampleIncorrectExtensionStartup","Microsoft.Azure.Functions.Worker.Core.WorkerExtensionStartup"),

                new DiagnosticResult(DiagnosticDescriptors.ConstructorMissing)
                .WithArguments("Worker.Extensions.Sample_IncorrectImplementation.SampleIncorrectExtensionStartup")
            };

            // override the namespace value for generated types using msbuild property.
            var buildPropertiesDict = new Dictionary<string, string>()
            {
                {  Constants.BuildProperties.GeneratedCodeNamespace, "MyCompany.MyProject.MyApp"}
            };

            await TestHelpers.RunTestAsync<ExtensionStartupRunnerGenerator>(
                referencedExtensionAssemblies,
                InputCode,
                expectedGeneratedFileName,
                expectedOutput,
                expectedDiagnosticResults,
                buildPropertiesDict);
        }
    }
}
