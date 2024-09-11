// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.SdkGeneratorTests.Helpers;
using Microsoft.Azure.Functions.Worker.Sdk.Generators;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Microsoft.Azure.Functions.SdkGeneratorTests
{
    public partial class FunctionExecutorGeneratorTests
    {
        public class DependentAssemblyTest
        {
            private readonly Assembly[] _referencedAssemblies;

            public DependentAssemblyTest()
            {
                var abstractionsExtension = Assembly.LoadFrom("Microsoft.Azure.Functions.Worker.Extensions.Abstractions.dll");
                var httpExtension = Assembly.LoadFrom("Microsoft.Azure.Functions.Worker.Extensions.Http.dll");
                var hostingExtension = typeof(HostBuilder).Assembly;
                var diExtension = typeof(DefaultServiceProviderFactory).Assembly;
                var hostingAbExtension = typeof(IHost).Assembly;
                var diAbExtension = typeof(IServiceCollection).Assembly;
                var dependentAssembly = Assembly.LoadFrom("DependentAssemblyWithFunctions.dll");

                _referencedAssemblies = new[]
                {
                    abstractionsExtension,
                    httpExtension,
                    hostingExtension,
                    hostingAbExtension,
                    diExtension,
                    diAbExtension,
                    dependentAssembly
                };
            }

            [Fact]
            public async Task FunctionsFromDependentAssembly()
            {
                await new FunctionExecutorGenerator()
                    .RunAndVerify("""
                        using System;
                        using Microsoft.Azure.Functions.Worker;
                        using Microsoft.Azure.Functions.Worker.Http;
                        namespace MyCompany
                        {
                            public class MyHttpTriggers
                            {
                                [Function("FunctionA")]
                                public HttpResponseData Foo([HttpTrigger(AuthorizationLevel.User, "get")] HttpRequestData r, FunctionContext c)
                                {
                                    return r.CreateResponse(System.Net.HttpStatusCode.OK);
                                }
                            }
                        }
                        """);
            }
        }
    }
}
