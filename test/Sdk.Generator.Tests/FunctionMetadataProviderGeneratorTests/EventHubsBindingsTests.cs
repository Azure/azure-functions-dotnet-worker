﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.Azure.Functions.Worker.Sdk.Generators;
using Xunit;

namespace Microsoft.Azure.Functions.SdkGeneratorTests
{
    public partial class FunctionMetadataProviderGeneratorTests
    {
        public class EventHubsBindingsTests
        {
            private Assembly[] referencedExtensionAssemblies;
            private readonly string _usingStringsForInput;

            public EventHubsBindingsTests()
            {
                var abstractionsExtension = Assembly.LoadFrom("Microsoft.Azure.Functions.Worker.Extensions.Abstractions.dll");
                var httpExtension = Assembly.LoadFrom("Microsoft.Azure.Functions.Worker.Extensions.Http.dll");
                var eventHubsExtension = Assembly.LoadFrom("Microsoft.Azure.Functions.Worker.Extensions.EventHubs.dll");
                var hostingExtension = Assembly.LoadFrom("Microsoft.Extensions.Hosting.dll");
                var diExtension = Assembly.LoadFrom("Microsoft.Extensions.DependencyInjection.dll");
                var hostingAbExtension = Assembly.LoadFrom("Microsoft.Extensions.Hosting.Abstractions.dll");
                var diAbExtension = Assembly.LoadFrom("Microsoft.Extensions.DependencyInjection.Abstractions.dll");

                referencedExtensionAssemblies = new[]
                {
                    abstractionsExtension,
                    httpExtension,
                    eventHubsExtension,
                    hostingExtension,
                    hostingAbExtension,
                    diExtension,
                    diAbExtension
                };

                _usingStringsForInput = @"using System;
                using System.Net;
                using System.Linq;
                using System.Text.Json;
                using System.Collections;
                using System.Collections.Concurrent;
                using System.Collections.Generic;
                using System.Threading.Tasks;
                using Microsoft.Azure.Functions.Worker;
                using Microsoft.Azure.Functions.Worker.Http;";
            }

            [Theory]
            [InlineData("StringInputFunction", "string", "String")]
            [InlineData("BinaryInputFunction", "byte[]", "Binary")]
            [InlineData("DictionaryInputFunction", "Dictionary<string, string>", "")]
            [InlineData("LookupFunction", "Lookup<string, int>", "")]
            [InlineData("ConcurrentDictionaryInputFunction", "ConcurrentDictionary<string, string>", "")]
            public async void FunctionsWithIsBatchedFalse(string functionName, string parameterType, string dataType)
            {
                StringBuilder inputCodeBuilder = new StringBuilder();
                inputCodeBuilder.Append(_usingStringsForInput);
                inputCodeBuilder.Append(@"
                namespace FunctionApp
                {
                    public class EventHubsInput
                    {");
                inputCodeBuilder.AppendLine("[Function(\"" + functionName + "\")]");
                inputCodeBuilder.AppendLine("public static void " + functionName + @"([EventHubTrigger(""test"", Connection = ""EventHubConnectionAppSetting"", IsBatched = false)]" + parameterType + " input, \n");
                inputCodeBuilder.Append(@"
                        FunctionContext context)
                        {
                            throw new NotImplementedException();
                        }
                    }
                }");

                string expectedGeneratedFileName = $"GeneratedFunctionMetadataProvider.g.cs";
                var expectedOutputBuilder = new StringBuilder();
                expectedOutputBuilder.Append(@"// <auto-generated/>
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
namespace Microsoft.Azure.Functions.Worker
{
    public class GeneratedFunctionMetadataProvider : IFunctionMetadataProvider
    {
        public Task<ImmutableArray<IFunctionMetadata>> GetFunctionMetadataAsync(string directory)
        {
            var metadataList = new List<IFunctionMetadata>();
            var Function0RawBindings = new List<string>();");
                expectedOutputBuilder.Append(@"
            Function0RawBindings.Add(@""{""""name"""":""""input"""",""""type"""":""""EventHubTrigger"""",""""direction"""":""""In"""",""""eventHubName"""":""""test"""",""""connection"""":""""EventHubConnectionAppSetting"""",""""cardinality"""":""""One""""");
                if(!string.Equals(dataType, ""))
                {
                    expectedOutputBuilder.Append(@",""""dataType"""":""""" + dataType + @"""""}"");");
                }
                else
                {
                    expectedOutputBuilder.Append(@"}"");");
                }

                expectedOutputBuilder.Append(@"
            var Function0 = new DefaultFunctionMetadata
            {
                Language = ""dotnet-isolated"",
                Name = """ + functionName + @""",
                EntryPoint = ""TestProject.EventHubsInput." + functionName + @""",
                RawBindings = Function0RawBindings,
                ScriptFile = ""TestProject.dll""
            };
            metadataList.Add(Function0);
            return Task.FromResult(metadataList.ToImmutableArray());
        }
    }
    public static class WorkerHostBuilderFunctionMetadataProviderExtension
    {
        ///<summary>
        /// Adds the GeneratedFunctionMetadataProvider to the service collection.
        /// During initialization, the worker will return generated function metadata instead of relying on the Azure Functions host for function indexing.
        ///</summary>
        public static IHostBuilder ConfigureGeneratedFunctionMetadataProvider(this IHostBuilder builder)
        {
            builder.ConfigureServices(s => 
            {
                s.AddSingleton<IFunctionMetadataProvider, GeneratedFunctionMetadataProvider>();
            });
            return builder;
        }
    }
}
");

                await TestHelpers.RunTestAsync<FunctionMetadataProviderGenerator>(
                    referencedExtensionAssemblies,
                    inputCodeBuilder.ToString(),
                    expectedGeneratedFileName,
                    expectedOutputBuilder.ToString());
            }

            [Theory]
            [InlineData("BinaryArrayInputFunction", "byte[][]", "Binary")]
            [InlineData("StringDoubleArrayInputFunction", "string[][]", "")]
            [InlineData("IntArrayInputFunction", "int[]", "")]
            [InlineData("StringArrayInputFunction", "string[]", "String")]
            [InlineData("StringListInputFunction", "List<string>", "String")]
            [InlineData("BinaryListInputFunction", "List<byte[]>", "Binary")]
            [InlineData("IntListInputFunction", "List<int>", "")]
            [InlineData("HashSetInputFunction", "HashSet<string>", "String")]
            [InlineData("EnumerableBinaryInputFunction", "IEnumerable<byte[]>", "Binary")]
            [InlineData("EnumerableStringInputFunction", "IEnumerable<string>", "String")]
            [InlineData("EnumerableInputFunction", "IEnumerable", "")]
            [InlineData("EnumerableClassInputFunction", "EnumerableTestClass", "")]
            [InlineData("EnumerableGenericClassInputFunction", "EnumerableGenericTestClass", "")]
            public async void FunctionsWithCardinalityMany(string functionName, string parameterType, string dataType)
            {
                StringBuilder inputCodeBuilder = new StringBuilder();
                inputCodeBuilder.Append(_usingStringsForInput);
                inputCodeBuilder.Append(@"
                namespace FunctionApp
                {
                    public class EventHubsInput
                    {");
                inputCodeBuilder.AppendLine("[Function(\"" + functionName + "\")]");
                inputCodeBuilder.AppendLine("public static void " + functionName + @"([EventHubTrigger(""test"", Connection = ""EventHubConnectionAppSetting"")]" + parameterType + " input, \n");
                inputCodeBuilder.Append(@"
                        FunctionContext context)
                        {
                            throw new NotImplementedException();
                        }
                    }

                    public class EnumerableTestClass : IEnumerable
                    {
                        public IEnumerator GetEnumerator()
                        {
                            throw new NotImplementedException();
                        }
                    }

                    public class EnumerableGenericTestClass : IEnumerable<int>
                    {
                        public IEnumerator GetEnumerator()
                        {
                            throw new NotImplementedException();
                        }

                        IEnumerator<int> IEnumerable<int>.GetEnumerator()
                        {
                            throw new NotImplementedException();
                        }
                    }
                }");

                string expectedGeneratedFileName = $"GeneratedFunctionMetadataProvider.g.cs";
                var expectedOutputBuilder = new StringBuilder();
                expectedOutputBuilder.Append(@"// <auto-generated/>
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
namespace Microsoft.Azure.Functions.Worker
{
    public class GeneratedFunctionMetadataProvider : IFunctionMetadataProvider
    {
        public Task<ImmutableArray<IFunctionMetadata>> GetFunctionMetadataAsync(string directory)
        {
            var metadataList = new List<IFunctionMetadata>();
            var Function0RawBindings = new List<string>();");

                expectedOutputBuilder.Append(@"
            Function0RawBindings.Add(@""{""""name"""":""""input"""",""""type"""":""""EventHubTrigger"""",""""direction"""":""""In"""",""""eventHubName"""":""""test"""",""""connection"""":""""EventHubConnectionAppSetting"""",""""cardinality"""":""""Many""""");
                if (!string.Equals(dataType, ""))
                {
                    expectedOutputBuilder.Append(@",""""dataType"""":""""" + dataType + @"""""}"");");
                }
                else
                {
                    expectedOutputBuilder.Append(@"}"");");
                }

                expectedOutputBuilder.Append(@"
            var Function0 = new DefaultFunctionMetadata
            {
                Language = ""dotnet-isolated"",
                Name = """ + functionName + @""",
                EntryPoint = ""TestProject.EventHubsInput." + functionName + @""",
                RawBindings = Function0RawBindings,
                ScriptFile = ""TestProject.dll""
            };
            metadataList.Add(Function0);
            return Task.FromResult(metadataList.ToImmutableArray());
        }
    }
    public static class WorkerHostBuilderFunctionMetadataProviderExtension
    {
        ///<summary>
        /// Adds the GeneratedFunctionMetadataProvider to the service collection.
        /// During initialization, the worker will return generated function metadata instead of relying on the Azure Functions host for function indexing.
        ///</summary>
        public static IHostBuilder ConfigureGeneratedFunctionMetadataProvider(this IHostBuilder builder)
        {
            builder.ConfigureServices(s => 
            {
                s.AddSingleton<IFunctionMetadataProvider, GeneratedFunctionMetadataProvider>();
            });
            return builder;
        }
    }
}
");

                await TestHelpers.RunTestAsync<FunctionMetadataProviderGenerator>(
                    referencedExtensionAssemblies,
                    inputCodeBuilder.ToString(),
                    expectedGeneratedFileName,
                    expectedOutputBuilder.ToString());
            }

            [Fact]
            public async void EnumerableGenericInputFunction()
            {
                string inputCode = @"
                using System;
                using System.Net;
                using System.Collections.Generic;
                using Microsoft.Azure.Functions.Worker;
                using Microsoft.Azure.Functions.Worker.Http;
                using System.Linq;
                using System.Threading.Tasks;

                namespace FunctionApp
                {
                    public class EventHubsInput
                    {
                        [Function('EnumerableBinaryInputFunction')]
                        public static void EnumerableBinaryInputFunction<T>([EventHubTrigger('test', Connection = 'EventHubConnectionAppSetting')] IEnumerable<T> input,
                            FunctionContext context)
                        {
                            throw new NotImplementedException();
                        }
                    }
                }
                ".Replace("'", "\"");

                string expectedGeneratedFileName = $"GeneratedFunctionMetadataProvider.g.cs";
                string expectedOutput = @"// <auto-generated/>
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
namespace Microsoft.Azure.Functions.Worker
{
    public class GeneratedFunctionMetadataProvider : IFunctionMetadataProvider
    {
        public Task<ImmutableArray<IFunctionMetadata>> GetFunctionMetadataAsync(string directory)
        {
            var metadataList = new List<IFunctionMetadata>();
            var Function0RawBindings = new List<string>();
            Function0RawBindings.Add(@""{""""name"""":""""input"""",""""type"""":""""EventHubTrigger"""",""""direction"""":""""In"""",""""eventHubName"""":""""test"""",""""connection"""":""""EventHubConnectionAppSetting"""",""""cardinality"""":""""Many""""}"");
            var Function0 = new DefaultFunctionMetadata
            {
                Language = 'dotnet-isolated',
                Name = 'EnumerableBinaryInputFunction',
                EntryPoint = 'TestProject.EventHubsInput.EnumerableBinaryInputFunction',
                RawBindings = Function0RawBindings,
                ScriptFile = 'TestProject.dll'
            };
            metadataList.Add(Function0);
            return Task.FromResult(metadataList.ToImmutableArray());
        }
    }
    public static class WorkerHostBuilderFunctionMetadataProviderExtension
    {
        ///<summary>
        /// Adds the GeneratedFunctionMetadataProvider to the service collection.
        /// During initialization, the worker will return generated function metadata instead of relying on the Azure Functions host for function indexing.
        ///</summary>
        public static IHostBuilder ConfigureGeneratedFunctionMetadataProvider(this IHostBuilder builder)
        {
            builder.ConfigureServices(s => 
            {
                s.AddSingleton<IFunctionMetadataProvider, GeneratedFunctionMetadataProvider>();
            });
            return builder;
        }
    }
}
".Replace("'", "\"");

                await TestHelpers.RunTestAsync<FunctionMetadataProviderGenerator>(
                    referencedExtensionAssemblies,
                    inputCode,
                    expectedGeneratedFileName,
                    expectedOutput);
            }

            [Fact]
            public async void EnumerableStringClassesAsInputFunctions()
            {
                string inputCode = @"
                using System;
                using System.Net;
                using System.Collections;
                using System.Collections.Generic;
                using Microsoft.Azure.Functions.Worker;
                using Microsoft.Azure.Functions.Worker.Http;
                using System.Linq;
                using System.Threading.Tasks;

                namespace FunctionApp
                {
                    public class EventHubsInput
                    {

                        [Function(""EnumerableStringClassInputFunction"")]
                        public static void EnumerableStringClassInputFunction([EventHubTrigger(""test"", Connection = ""EventHubConnectionAppSetting"")] EnumerableStringTestClass input,
                            FunctionContext context)
                        {
                            throw new NotImplementedException();
                        }

                        [Function(""EnumerableNestedStringClassInputFunction"")]
                        public static void EnumerableNestedStringClassInputFunction([EventHubTrigger(""test"", Connection = ""EventHubConnectionAppSetting"")] EnumerableStringNestedTestClass input,
                        FunctionContext context)
                        {
                            throw new NotImplementedException();
                        }

                        [Function(""EnumerableNestedStringGenericClassInputFunction"")]
                        public static void EnumerableNestedStringGenericClassInputFunction([EventHubTrigger(""test"", Connection = ""EventHubConnectionAppSetting"")] EnumerableStringNestedGenericTestClass<string> input,
                            FunctionContext context)
                        {
                            throw new NotImplementedException();
                        }

                        [Function(""EnumerableNestedStringGenericClass2InputFunction"")]
                        public static void EnumerableNestedStringGenericClass2InputFunction([EventHubTrigger(""test"", Connection = ""EventHubConnectionAppSetting"")] EnumerableStringNestedGenericTestClass2<string, int> input,
                            FunctionContext context)
                        {
                            throw new NotImplementedException();
                        }

                        public class EnumerableStringTestClass : IEnumerable<string>
                        {
                            public IEnumerator GetEnumerator()
                            {
                                throw new NotImplementedException();
                            }

                            IEnumerator<string> IEnumerable<string>.GetEnumerator()
                            {
                                throw new NotImplementedException();
                            }
                        }

                        public class EnumerableStringTestClass<T> : List<T>
                        {
                        }

                        public class EnumerableStringNestedTestClass : EnumerableStringTestClass
                        {
                        }

                        public class EnumerableStringNestedGenericTestClass2<T, V> : EnumerableStringNestedGenericTestClass<T>
                        {
                        }

                        public class EnumerableStringNestedGenericTestClass<TK> : EnumerableStringTestClass<TK>
                        {
                        }
                    }
                }
                ".Replace("'", "\"");

                string expectedGeneratedFileName = $"GeneratedFunctionMetadataProvider.g.cs";
                string expectedOutput = @"// <auto-generated/>
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
namespace Microsoft.Azure.Functions.Worker
{
    public class GeneratedFunctionMetadataProvider : IFunctionMetadataProvider
    {
        public Task<ImmutableArray<IFunctionMetadata>> GetFunctionMetadataAsync(string directory)
        {
            var metadataList = new List<IFunctionMetadata>();
            var Function0RawBindings = new List<string>();
            Function0RawBindings.Add(@""{""""name"""":""""input"""",""""type"""":""""EventHubTrigger"""",""""direction"""":""""In"""",""""eventHubName"""":""""test"""",""""connection"""":""""EventHubConnectionAppSetting"""",""""cardinality"""":""""Many"""",""""dataType"""":""""String""""}"");
            var Function0 = new DefaultFunctionMetadata
            {
                Language = 'dotnet-isolated',
                Name = 'EnumerableStringClassInputFunction',
                EntryPoint = 'TestProject.EventHubsInput.EnumerableStringClassInputFunction',
                RawBindings = Function0RawBindings,
                ScriptFile = 'TestProject.dll'
            };
            metadataList.Add(Function0);
            var Function1RawBindings = new List<string>();
            Function1RawBindings.Add(@""{""""name"""":""""input"""",""""type"""":""""EventHubTrigger"""",""""direction"""":""""In"""",""""eventHubName"""":""""test"""",""""connection"""":""""EventHubConnectionAppSetting"""",""""cardinality"""":""""Many"""",""""dataType"""":""""String""""}"");
            var Function1 = new DefaultFunctionMetadata
            {
                Language = 'dotnet-isolated',
                Name = 'EnumerableNestedStringClassInputFunction',
                EntryPoint = 'TestProject.EventHubsInput.EnumerableNestedStringClassInputFunction',
                RawBindings = Function1RawBindings,
                ScriptFile = 'TestProject.dll'
            };
            metadataList.Add(Function1);
            var Function2RawBindings = new List<string>();
            Function2RawBindings.Add(@""{""""name"""":""""input"""",""""type"""":""""EventHubTrigger"""",""""direction"""":""""In"""",""""eventHubName"""":""""test"""",""""connection"""":""""EventHubConnectionAppSetting"""",""""cardinality"""":""""Many"""",""""dataType"""":""""String""""}"");
            var Function2 = new DefaultFunctionMetadata
            {
                Language = 'dotnet-isolated',
                Name = 'EnumerableNestedStringGenericClassInputFunction',
                EntryPoint = 'TestProject.EventHubsInput.EnumerableNestedStringGenericClassInputFunction',
                RawBindings = Function2RawBindings,
                ScriptFile = 'TestProject.dll'
            };
            metadataList.Add(Function2);
            var Function3RawBindings = new List<string>();
            Function3RawBindings.Add(@""{""""name"""":""""input"""",""""type"""":""""EventHubTrigger"""",""""direction"""":""""In"""",""""eventHubName"""":""""test"""",""""connection"""":""""EventHubConnectionAppSetting"""",""""cardinality"""":""""Many"""",""""dataType"""":""""String""""}"");
            var Function3 = new DefaultFunctionMetadata
            {
                Language = 'dotnet-isolated',
                Name = 'EnumerableNestedStringGenericClass2InputFunction',
                EntryPoint = 'TestProject.EventHubsInput.EnumerableNestedStringGenericClass2InputFunction',
                RawBindings = Function3RawBindings,
                ScriptFile = 'TestProject.dll'
            };
            metadataList.Add(Function3);
            return Task.FromResult(metadataList.ToImmutableArray());
        }
    }
    public static class WorkerHostBuilderFunctionMetadataProviderExtension
    {
        ///<summary>
        /// Adds the GeneratedFunctionMetadataProvider to the service collection.
        /// During initialization, the worker will return generated function metadata instead of relying on the Azure Functions host for function indexing.
        ///</summary>
        public static IHostBuilder ConfigureGeneratedFunctionMetadataProvider(this IHostBuilder builder)
        {
            builder.ConfigureServices(s => 
            {
                s.AddSingleton<IFunctionMetadataProvider, GeneratedFunctionMetadataProvider>();
            });
            return builder;
        }
    }
}
".Replace("'", "\"");

                await TestHelpers.RunTestAsync<FunctionMetadataProviderGenerator>(
                    referencedExtensionAssemblies,
                    inputCode,
                    expectedGeneratedFileName,
                    expectedOutput);
            }

            [Fact]
            public async void EnumerableBinaryClassesAsInputFunctions()
            {
                string inputCode = @"
                using System;
                using System.Net;
                using System.Collections;
                using System.Collections.Generic;
                using Microsoft.Azure.Functions.Worker;
                using Microsoft.Azure.Functions.Worker.Http;
                using System.Linq;
                using System.Threading.Tasks;

                namespace FunctionApp
                {
                    public class EventHubsInput
                    {
                        [Function(""EnumerableBinaryClassInputFunction"")]
                        public static void EnumerableBinaryClassInputFunction([EventHubTrigger(""test"", Connection = ""EventHubConnectionAppSetting"")] EnumerableBinaryTestClass input,
                            FunctionContext context)
                        {
                            throw new NotImplementedException();
                        }

                        [Function(""EnumerableNestedBinaryClassInputFunction"")]
                        public static void EnumerableNestedBinaryClassInputFunction([EventHubTrigger(""test"", Connection = ""EventHubConnectionAppSetting"")] EnumerableBinaryNestedTestClass input,
                            FunctionContext context)
                        {
                            throw new NotImplementedException();
                        }

                        public class EnumerableBinaryTestClass : IEnumerable<byte[]>
                        {
                            public IEnumerator GetEnumerator()
                            {
                                throw new NotImplementedException();
                            }

                            IEnumerator<byte[]> IEnumerable<byte[]>.GetEnumerator()
                            {
                                throw new NotImplementedException();
                            }
                        }

                        public class EnumerableBinaryNestedTestClass : EnumerableBinaryTestClass
                        {
                        }
                    }
                }
                ".Replace("'", "\"");

                string expectedGeneratedFileName = $"GeneratedFunctionMetadataProvider.g.cs";
                string expectedOutput = @"// <auto-generated/>
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
namespace Microsoft.Azure.Functions.Worker
{
    public class GeneratedFunctionMetadataProvider : IFunctionMetadataProvider
    {
        public Task<ImmutableArray<IFunctionMetadata>> GetFunctionMetadataAsync(string directory)
        {
            var metadataList = new List<IFunctionMetadata>();
            var Function0RawBindings = new List<string>();
            Function0RawBindings.Add(@""{""""name"""":""""input"""",""""type"""":""""EventHubTrigger"""",""""direction"""":""""In"""",""""eventHubName"""":""""test"""",""""connection"""":""""EventHubConnectionAppSetting"""",""""cardinality"""":""""Many"""",""""dataType"""":""""Binary""""}"");
            var Function0 = new DefaultFunctionMetadata
            {
                Language = 'dotnet-isolated',
                Name = 'EnumerableBinaryClassInputFunction',
                EntryPoint = 'TestProject.EventHubsInput.EnumerableBinaryClassInputFunction',
                RawBindings = Function0RawBindings,
                ScriptFile = 'TestProject.dll'
            };
            metadataList.Add(Function0);
            var Function1RawBindings = new List<string>();
            Function1RawBindings.Add(@""{""""name"""":""""input"""",""""type"""":""""EventHubTrigger"""",""""direction"""":""""In"""",""""eventHubName"""":""""test"""",""""connection"""":""""EventHubConnectionAppSetting"""",""""cardinality"""":""""Many"""",""""dataType"""":""""Binary""""}"");
            var Function1 = new DefaultFunctionMetadata
            {
                Language = 'dotnet-isolated',
                Name = 'EnumerableNestedBinaryClassInputFunction',
                EntryPoint = 'TestProject.EventHubsInput.EnumerableNestedBinaryClassInputFunction',
                RawBindings = Function1RawBindings,
                ScriptFile = 'TestProject.dll'
            };
            metadataList.Add(Function1);
            return Task.FromResult(metadataList.ToImmutableArray());
        }
    }
    public static class WorkerHostBuilderFunctionMetadataProviderExtension
    {
        ///<summary>
        /// Adds the GeneratedFunctionMetadataProvider to the service collection.
        /// During initialization, the worker will return generated function metadata instead of relying on the Azure Functions host for function indexing.
        ///</summary>
        public static IHostBuilder ConfigureGeneratedFunctionMetadataProvider(this IHostBuilder builder)
        {
            builder.ConfigureServices(s => 
            {
                s.AddSingleton<IFunctionMetadataProvider, GeneratedFunctionMetadataProvider>();
            });
            return builder;
        }
    }
}
".Replace("'", "\"");

                await TestHelpers.RunTestAsync<FunctionMetadataProviderGenerator>(
                    referencedExtensionAssemblies,
                    inputCode,
                    expectedGeneratedFileName,
                    expectedOutput);
            }

            [Fact]
            public async void PocoInputFunctions()
            {
                string inputCode = @"
                using System;
                using System.Net;
                using System.Collections;
                using System.Collections.Generic;
                using Microsoft.Azure.Functions.Worker;
                using Microsoft.Azure.Functions.Worker.Http;
                using System.Linq;
                using System.Threading.Tasks;

                namespace FunctionApp
                {
                    public class EventHubsInput
                    {
                        [Function(""EnumerablePocoInputFunction"")]
                        public static void EnumerablePocoInputFunction<T>([EventHubTrigger(""test"", Connection = ""EventHubConnectionAppSetting"")] IEnumerable<Poco> input,
                        FunctionContext context)
                        {
                            throw new NotImplementedException();
                        }

                        [Function(""ListPocoInputFunction"")]
                        public static void ListPocoInputFunction<T>([EventHubTrigger(""test"", Connection = ""EventHubConnectionAppSetting"")] List<Poco> input,
                            FunctionContext context)
                        {
                            throw new NotImplementedException();
                        }

                        public class Poco
                        {
                        }
                    }
                }
                ".Replace("'", "\"");

                string expectedGeneratedFileName = $"GeneratedFunctionMetadataProvider.g.cs";
                string expectedOutput = @"// <auto-generated/>
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
namespace Microsoft.Azure.Functions.Worker
{
    public class GeneratedFunctionMetadataProvider : IFunctionMetadataProvider
    {
        public Task<ImmutableArray<IFunctionMetadata>> GetFunctionMetadataAsync(string directory)
        {
            var metadataList = new List<IFunctionMetadata>();
            var Function0RawBindings = new List<string>();
            Function0RawBindings.Add(@""{""""name"""":""""input"""",""""type"""":""""EventHubTrigger"""",""""direction"""":""""In"""",""""eventHubName"""":""""test"""",""""connection"""":""""EventHubConnectionAppSetting"""",""""cardinality"""":""""Many""""}"");
            var Function0 = new DefaultFunctionMetadata
            {
                Language = 'dotnet-isolated',
                Name = 'EnumerablePocoInputFunction',
                EntryPoint = 'TestProject.EventHubsInput.EnumerablePocoInputFunction',
                RawBindings = Function0RawBindings,
                ScriptFile = 'TestProject.dll'
            };
            metadataList.Add(Function0);
            var Function1RawBindings = new List<string>();
            Function1RawBindings.Add(@""{""""name"""":""""input"""",""""type"""":""""EventHubTrigger"""",""""direction"""":""""In"""",""""eventHubName"""":""""test"""",""""connection"""":""""EventHubConnectionAppSetting"""",""""cardinality"""":""""Many""""}"");
            var Function1 = new DefaultFunctionMetadata
            {
                Language = 'dotnet-isolated',
                Name = 'ListPocoInputFunction',
                EntryPoint = 'TestProject.EventHubsInput.ListPocoInputFunction',
                RawBindings = Function1RawBindings,
                ScriptFile = 'TestProject.dll'
            };
            metadataList.Add(Function1);
            return Task.FromResult(metadataList.ToImmutableArray());
        }
    }
    public static class WorkerHostBuilderFunctionMetadataProviderExtension
    {
        ///<summary>
        /// Adds the GeneratedFunctionMetadataProvider to the service collection.
        /// During initialization, the worker will return generated function metadata instead of relying on the Azure Functions host for function indexing.
        ///</summary>
        public static IHostBuilder ConfigureGeneratedFunctionMetadataProvider(this IHostBuilder builder)
        {
            builder.ConfigureServices(s => 
            {
                s.AddSingleton<IFunctionMetadataProvider, GeneratedFunctionMetadataProvider>();
            });
            return builder;
        }
    }
}
".Replace("'", "\"");

                await TestHelpers.RunTestAsync<FunctionMetadataProviderGenerator>(
                    referencedExtensionAssemblies,
                    inputCode,
                    expectedGeneratedFileName,
                    expectedOutput);
            }

            [Fact]
            public async void CardinalityManyWithNonIterableInputFails()
            {
                var inputCode = @"using System;
                using System.Net;
                using System.Collections;
                using System.Collections.Generic;
                using Microsoft.Azure.Functions.Worker;
                using Microsoft.Azure.Functions.Worker.Http;
                using System.Linq;
                using System.Threading.Tasks;

                namespace FunctionApp
                {
                    public class EventHubsInput
                    {
                        [Function(""InvalidEventHubsTrigger"")]
                        public static void InvalidEventHubsTrigger([EventHubTrigger(""test"", Connection = ""EventHubConnectionAppSetting"")] string input,
                            FunctionContext context)
                        {
                            throw new NotImplementedException();
                        }
                    }
                }";

                string? expectedGeneratedFileName = null;
                string? expectedOutput = null;

                await TestHelpers.RunTestAsync<ExtensionStartupRunnerGenerator>(
                    referencedExtensionAssemblies,
                    inputCode,
                    expectedGeneratedFileName,
                    expectedOutput);
            }
        }
    }
}
