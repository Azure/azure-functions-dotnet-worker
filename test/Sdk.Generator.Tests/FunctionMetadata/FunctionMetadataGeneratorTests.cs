// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Azure.Messaging.EventHubs;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Azure.Storage.Queues.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Tests;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Sdk;
using Mono.Cecil;
using Xunit;

namespace Microsoft.Azure.Functions.Sdk.Generator.FunctionMetadata.Tests
{
    public class FunctionMetadataGeneratorTests
    {
        private static Assembly _thisAssembly = typeof(FunctionMetadataGeneratorTests).Assembly;

        [Fact]
        public void BasicHttpFunction()
        {
            var generator = new FunctionMetadataGenerator();
            var module = ModuleDefinition.ReadModule(_thisAssembly.Location);
            var typeDef = TypeHelpers.GetTypeDefinition(typeof(BasicHttp));
            var functions = generator.GenerateFunctionMetadata(typeDef);
            var extensions = generator.Extensions;

            AssertDictionary(extensions, new Dictionary<string, string>
            {
            });

            ValidateFunction(functions.Single(), BasicHttp.FunctionName, GetEntryPoint(nameof(BasicHttp), nameof(BasicHttp.Http)),
                b => ValidateTrigger(b),
                b => ValidateReturn(b));

            void ValidateTrigger(ExpandoObject b)
            {
                AssertExpandoObject(b, new Dictionary<string, object>
                {
                    { "Name", "myReq" },
                    { "Type", "httpTrigger" },
                    { "Direction", "In" },
                    { "authLevel", "Admin" },
                    { "methods", new[] { "get", "Post" } },
                    { "Route", "/api2" },
                    { "Properties", new Dictionary<String, Object>() }
                });
            }

            void ValidateReturn(ExpandoObject b)
            {
                AssertExpandoObject(b, new Dictionary<string, object>
                {
                    { "Name", "$return" },
                    { "Type", "http" },
                    { "Direction", "Out" }
                });
            }

            FunctionMetadataJsonWriter.WriteMetadata(functions, ".");
        }

        [Fact]
        public void BasicHttpAspNetFunction()
        {
            var generator = new FunctionMetadataGenerator();
            var module = ModuleDefinition.ReadModule(_thisAssembly.Location);
            var typeDef = TypeHelpers.GetTypeDefinition(typeof(BasicHttpAspNet));
            var functions = generator.GenerateFunctionMetadata(typeDef);
            var extensions = generator.Extensions;

            AssertDictionary(extensions, new Dictionary<string, string>
            {
            });

            ValidateFunction(functions.Single(), BasicHttpAspNet.FunctionName, GetEntryPoint(nameof(BasicHttpAspNet), nameof(BasicHttpAspNet.Http)),
                b => ValidateTrigger(b),
                b => ValidateReturn(b));

            void ValidateTrigger(ExpandoObject b)
            {
                AssertExpandoObject(b, new Dictionary<string, object>
                {
                    { "Name", "myReq" },
                    { "Type", "httpTrigger" },
                    { "Direction", "In" },
                    { "authLevel", "Admin" },
                    { "methods", new[] { "get", "Post" } },
                    { "Route", "/api2" },
                    { "Properties", new Dictionary<String, Object>() }
                });
            }

            void ValidateReturn(ExpandoObject b)
            {
                AssertExpandoObject(b, new Dictionary<string, object>
                {
                    { "Name", "$return" },
                    { "Type", "http" },
                    { "Direction", "Out" }
                });
            }

            FunctionMetadataJsonWriter.WriteMetadata(functions, ".");
        }

        [Fact]
        public void BasicHttpFunctionWithNoResponse()
        {
            var generator = new FunctionMetadataGenerator();
            var module = ModuleDefinition.ReadModule(_thisAssembly.Location);
            var typeDef = TypeHelpers.GetTypeDefinition(typeof(BasicHttpWithNoResponse));
            var functions = generator.GenerateFunctionMetadata(typeDef);
            var extensions = generator.Extensions;

            AssertDictionary(extensions, new Dictionary<string, string>
            {
            });

            ValidateFunction(functions.Single(), BasicHttpWithNoResponse.FunctionName, GetEntryPoint(nameof(BasicHttpWithNoResponse), nameof(BasicHttpWithNoResponse.Http)),
                b => ValidateTrigger(b));

            void ValidateTrigger(ExpandoObject b)
            {
                AssertExpandoObject(b, new Dictionary<string, object>
                {
                    { "Name", "myReq" },
                    { "Type", "httpTrigger" },
                    { "Direction", "In" },
                    { "authLevel", "Admin" },
                    { "methods", new[] { "get", "Post" } },
                    { "Route", "/api2" },
                    { "Properties", new Dictionary<String, Object>() }
                });
            }

            FunctionMetadataJsonWriter.WriteMetadata(functions, ".");
        }

        [Fact]
        public void BasicHttpFunctionWithExternalReturnType()
        {
            var generator = new FunctionMetadataGenerator();
            var module = ModuleDefinition.ReadModule(_thisAssembly.Location);
            var typeDef = TypeHelpers.GetTypeDefinition(typeof(ExternalType_Return));
            var functions = generator.GenerateFunctionMetadata(typeDef);
            var extensions = generator.Extensions;

            AssertDictionary(extensions, new Dictionary<string, string>
            {
            });

            ValidateFunction(functions.Single(), ExternalType_Return.FunctionName, GetEntryPoint(nameof(ExternalType_Return), nameof(ExternalType_Return.Http)),
                b => ValidateTrigger(b),
                b => ValidateQueueOutput(b));

            void ValidateTrigger(ExpandoObject b)
            {
                AssertExpandoObject(b, new Dictionary<string, object>
                {
                    { "Name", "myReq" },
                    { "Type", "httpTrigger" },
                    { "Direction", "In" },
                    { "authLevel", "Admin" },
                    { "methods", new[] { "get", "Post" } },
                    { "Route", "/api2" },
                    { "Properties", new Dictionary<String, Object>() }
                });
            }

            void ValidateQueueOutput(ExpandoObject b)
            {
                AssertExpandoObject(b, new Dictionary<string, object>
                {
                    { "Name", "$return" },
                    { "Type", "http" },
                    { "Direction", "Out" }
                });
            }

            FunctionMetadataJsonWriter.WriteMetadata(functions, ".");
        }

        [Fact]
        public void StorageFunctions()
        {
            var generator = new FunctionMetadataGenerator();
            var module = ModuleDefinition.ReadModule(_thisAssembly.Location);
            var typeDef = TypeHelpers.GetTypeDefinition(typeof(Storage));
            var functions = generator.GenerateFunctionMetadata(typeDef);
            var extensions = generator.Extensions;

            Assert.Equal(3, functions.Count());

            var queueToBlob = functions.Single(p => p.Name == "QueueToBlobFunction");
            var blobToQueue = functions.Single(p => p.Name == "BlobToQueueFunction");
            var blobToBlobs = functions.Single(p => p.Name == "BlobToBlobsFunction");

            ValidateFunction(queueToBlob, "QueueToBlobFunction", GetEntryPoint(nameof(Storage), nameof(Storage.QueueToBlob)),
                b => ValidateQueueTrigger(b),
                b => ValidateBlobOutput(b));

            ValidateFunction(blobToBlobs, "BlobToBlobsFunction", GetEntryPoint(nameof(Storage), nameof(Storage.BlobToBlobs)),
                b => ValidateBlobTrigger(b),
                b => ValidateBlobInput(b),
                b => ValidateBlobOutput(b));

            AssertDictionary(extensions, new Dictionary<string, string>
            {
                { "Microsoft.Azure.WebJobs.Extensions.Storage.Queues", "5.3.1" },
                { "Microsoft.Azure.WebJobs.Extensions.Storage.Blobs", "5.3.1" },
            });

            void ValidateQueueTrigger(ExpandoObject b)
            {
                AssertExpandoObject(b, new Dictionary<string, object>
                {
                    { "Name", "queuePayload" },
                    { "Type", "queueTrigger" },
                    { "Direction", "In" },
                    { "Connection", "MyConnection" },
                    { "queueName", "queueName" },
                    { "DataType", "String" },
                    { "Properties", new Dictionary<String, Object>() }
                });
            }

            void ValidateBlobOutput(ExpandoObject b)
            {
                AssertExpandoObject(b, new Dictionary<string, object>
                {
                    { "Name", "$return" },
                    { "Type", "blob" },
                    { "Direction", "Out" },
                    { "blobPath", "container1/hello.txt" },
                    { "Connection", "MyOtherConnection" },
                    { "Properties", new Dictionary<String, Object>() }
                });
            }

            void ValidateBlobInput(ExpandoObject b)
            {
                AssertExpandoObject(b, new Dictionary<string, object>
                {
                    { "Name", "blobinput" },
                    { "Type", "blob" },
                    { "Direction", "In" },
                    { "blobPath", "container2" },
                    { "Properties", new Dictionary<String, Object>( ) { { "SupportsDeferredBinding" , "True"} } }
                });
            }

            ValidateFunction(blobToQueue, "BlobToQueueFunction", GetEntryPoint(nameof(Storage), nameof(Storage.BlobToQueue)),
                b => ValidateBlobTrigger(b),
                b => ValidateQueueOutput(b));

            void ValidateBlobTrigger(ExpandoObject b)
            {
                AssertExpandoObject(b, new Dictionary<string, object>
                {
                    { "Name", "blob" },
                    { "Type", "blobTrigger" },
                    { "Direction", "In" },
                    { "path", "container2/%file%" },
                    { "Properties", new Dictionary<String, Object>( ) { { "SupportsDeferredBinding" , "True"} } }
                });
            }

            void ValidateQueueOutput(ExpandoObject b)
            {
                AssertExpandoObject(b, new Dictionary<string, object>
                {
                    { "Name", "$return" },
                    { "Type", "queue" },
                    { "Direction", "Out" },
                    { "queueName", "queue2" },
                    { "Properties", new Dictionary<String, Object>() }
                });
            }
        }

        [Fact]
        public void BlobStorageFunctions_SDKTypeBindings()
        {
            var generator = new FunctionMetadataGenerator();
            var module = ModuleDefinition.ReadModule(_thisAssembly.Location);
            var typeDef = TypeHelpers.GetTypeDefinition(typeof(SDKTypeBindings_BlobStorage));
            var functions = generator.GenerateFunctionMetadata(typeDef);
            var extensions = generator.Extensions;

            Assert.Equal(5, functions.Count());

            var blobStringToBlobStringFunction = functions.Single(p => p.Name == "BlobStringToBlobStringFunction");

            ValidateFunction(blobStringToBlobStringFunction, "BlobStringToBlobStringFunction", GetEntryPoint(nameof(SDKTypeBindings_BlobStorage), nameof(SDKTypeBindings_BlobStorage.BlobStringToBlobStringFunction)),
                b => ValidateBlobTrigger(b),
                b => ValidateBlobInput(b),
                b => ValidateBlobOutput(b));

            AssertDictionary(extensions, new Dictionary<string, string>
            {
                { "Microsoft.Azure.WebJobs.Extensions.Storage.Blobs", "5.3.1" },
            });

            var blobClientToBlobStringFunction = functions.Single(p => p.Name == "BlobClientToBlobStringFunction");

            ValidateFunction(blobClientToBlobStringFunction, "BlobClientToBlobStringFunction", GetEntryPoint(nameof(SDKTypeBindings_BlobStorage), nameof(SDKTypeBindings_BlobStorage.BlobClientToBlobStreamFunction)),
                b => ValidateBlobTrigger(b),
                b => ValidateBlobInput(b),
                b => ValidateBlobOutput(b));


            var blobUnsupportedTypeToBlobStringFunction = functions.Single(p => p.Name == "BlobUnsupportedTypeToBlobStringFunction");

            ValidateFunction(blobUnsupportedTypeToBlobStringFunction, "BlobUnsupportedTypeToBlobStringFunction", GetEntryPoint(nameof(SDKTypeBindings_BlobStorage), nameof(SDKTypeBindings_BlobStorage.BlobUnsupportedTypeToBlobClientFunction)),
                b => ValidateBlobTrigger(b),
                b => ValidateBlobInput(b),
                b => ValidateBlobOutput(b));


            var blobPocoToBlobUnsupportedType = functions.Single(p => p.Name == "BlobPocoToBlobUnsupportedType");

            ValidateFunction(blobPocoToBlobUnsupportedType, "BlobPocoToBlobUnsupportedType", GetEntryPoint(nameof(SDKTypeBindings_BlobStorage), nameof(SDKTypeBindings_BlobStorage.BlobPocoToBlobUnsupportedType)),
                b => ValidateBlobTrigger(b),
                b => ValidateBlobInput(b),
                b => ValidateBlobOutput(b));


            var blobByteArrayToBlobByteArrayFunction = functions.Single(p => p.Name == "BlobByteArrayToBlobByteArrayFunction");

            ValidateFunction(blobByteArrayToBlobByteArrayFunction, "BlobByteArrayToBlobByteArrayFunction", GetEntryPoint(nameof(SDKTypeBindings_BlobStorage), nameof(SDKTypeBindings_BlobStorage.BlobByteArrayToBlobByteArrayFunction)),
                b => ValidateBlobTrigger(b),
                b => ValidateBlobInput(b),
                b => ValidateBlobOutput(b));


            void ValidateBlobTrigger(ExpandoObject b)
            {
                AssertExpandoObject(b, new Dictionary<string, object>
                {
                    { "Name", "blob" },
                    { "Type", "blobTrigger" },
                    { "Direction", "In" },
                    { "path", "container2/%file%" },
                    { "Properties", new Dictionary<String, Object>( ) { { "SupportsDeferredBinding" , "True"} } }
                });
            }

            void ValidateBlobInput(ExpandoObject b)
            {
                AssertExpandoObject(b, new Dictionary<string, object>
                {
                    { "Name", "blobinput" },
                    { "Type", "blob" },
                    { "Direction", "In" },
                    { "blobPath", "container2/%file%" },
                    { "Properties", new Dictionary<String, Object>( ) { { "SupportsDeferredBinding" , "True"} } }
                });
            }

            void ValidateBlobOutput(ExpandoObject b)
            {
                AssertExpandoObject(b, new Dictionary<string, object>
                {
                    { "Name", "$return" },
                    { "Type", "blob" },
                    { "Direction", "Out" },
                    { "blobPath", "container1/hello.txt" },
                    { "Connection", "MyOtherConnection" },
                    { "Properties", new Dictionary<String, Object>() }
                });
            }
        }

        [Fact]
        public void BlobCollectionFunctions_SDKTypeBindings()
        {
            var generator = new FunctionMetadataGenerator();
            var module = ModuleDefinition.ReadModule(_thisAssembly.Location);
            var typeDef = TypeHelpers.GetTypeDefinition(typeof(SDKTypeBindings_BlobCollection));
            var functions = generator.GenerateFunctionMetadata(typeDef);
            var extensions = generator.Extensions;

            Assert.Equal(4, functions.Count());

            AssertDictionary(extensions, new Dictionary<string, string>
            {
                { "Microsoft.Azure.WebJobs.Extensions.Storage.Blobs", "5.3.1" },
            });

            var blobStringToBlobClientEnumerable = functions.Single(p => p.Name == "BlobStringToBlobClientEnumerable");

            ValidateFunction(blobStringToBlobClientEnumerable, "BlobStringToBlobClientEnumerable", GetEntryPoint(nameof(SDKTypeBindings_BlobCollection), nameof(SDKTypeBindings_BlobCollection.BlobStringToBlobClientEnumerable)),
                b => ValidateBlobTrigger(b),
                b => ValidateBlobInputForEnumerable(b),
                b => ValidateBlobOutput(b));

            void ValidateBlobInputForEnumerable(ExpandoObject b)
            {
                AssertExpandoObject(b, new Dictionary<string, object>
                {
                    { "Name", "blobinput" },
                    { "Type", "blob" },
                    { "Direction", "In" },
                    { "blobPath", "container2" },
                    { "Properties", new Dictionary<String, Object>( ) { { "SupportsDeferredBinding", "True" } } }
                });
            }

            var blobStringToBlobStringArray = functions.Single(p => p.Name == "BlobStringToBlobStringArray");

            ValidateFunction(blobStringToBlobStringArray, "BlobStringToBlobStringArray", GetEntryPoint(nameof(SDKTypeBindings_BlobCollection), nameof(SDKTypeBindings_BlobCollection.BlobStringToBlobStringArray)),
                b => ValidateBlobTrigger(b),
                b => ValidateBlobInputForStringArray(b),
                b => ValidateBlobOutput(b));

            void ValidateBlobInputForStringArray(ExpandoObject b)
            {
                AssertExpandoObject(b, new Dictionary<string, object>
                {
                    { "Name", "blobinput" },
                    { "Type", "blob" },
                    { "Direction", "In" },
                    { "blobPath", "container2" },
                    { "Properties", new Dictionary<String, Object>( ) { { "SupportsDeferredBinding", "True" } } }
                });
            }

            var blobStringToBlobPocoEnumerable = functions.Single(p => p.Name == "BlobStringToBlobPocoEnumerable");

            ValidateFunction(blobStringToBlobPocoEnumerable, "BlobStringToBlobPocoEnumerable", GetEntryPoint(nameof(SDKTypeBindings_BlobCollection), nameof(SDKTypeBindings_BlobCollection.BlobStringToBlobPocoEnumerable)),
                b => ValidateBlobTrigger(b),
                b => ValidateBlobInputForEnumerable(b),
                b => ValidateBlobOutput(b));


            var blobStringToBlobPocoArray = functions.Single(p => p.Name == "BlobStringToBlobPocoArray");

            ValidateFunction(blobStringToBlobPocoArray, "BlobStringToBlobPocoArray", GetEntryPoint(nameof(SDKTypeBindings_BlobCollection), nameof(SDKTypeBindings_BlobCollection.BlobStringToBlobPocoArray)),
                b => ValidateBlobTrigger(b),
                b => ValidateBlobInputForEnumerable(b),
                b => ValidateBlobOutput(b));


            void ValidateBlobTrigger(ExpandoObject b)
            {
                AssertExpandoObject(b, new Dictionary<string, object>
                {
                    { "Name", "blob" },
                    { "Type", "blobTrigger" },
                    { "Direction", "In" },
                    { "path", "container2/%file%" },
                    { "Properties", new Dictionary<String, Object>( ) { { "SupportsDeferredBinding" , "True"} } }
                });
            }

            void ValidateBlobOutput(ExpandoObject b)
            {
                AssertExpandoObject(b, new Dictionary<string, object>
                {
                    { "Name", "$return" },
                    { "Type", "blob" },
                    { "Direction", "Out" },
                    { "blobPath", "container1/hello.txt" },
                    { "Connection", "MyOtherConnection" },
                    { "Properties", new Dictionary<String, Object>() }
                });
            }
        }

        [Fact]
        public void TableFunctions_SDKTypeBindings()
        {
            var generator = new FunctionMetadataGenerator();
            var module = ModuleDefinition.ReadModule(_thisAssembly.Location);
            var typeDef = TypeHelpers.GetTypeDefinition(typeof(SDKTypeBindings_Table));
            var functions = generator.GenerateFunctionMetadata(typeDef);
            var extensions = generator.Extensions;

            Assert.Equal(5, functions.Count());

            var tableClientFunction = functions.Single(p => p.Name == "TableClientFunction");

            ValidateFunction(tableClientFunction, "TableClientFunction", GetEntryPoint(nameof(SDKTypeBindings_Table), nameof(SDKTypeBindings_Table.TableClientFunction)),
                b => ValidateTableInput(b));

            AssertDictionary(extensions, new Dictionary<string, string>
            {
                { "Microsoft.Azure.WebJobs.Extensions.Tables", "1.3.2" },
            });

            var tableEntityFunction = functions.Single(p => p.Name == "TableEntityFunction");

            ValidateFunction(tableEntityFunction, "TableEntityFunction", GetEntryPoint(nameof(SDKTypeBindings_Table), nameof(SDKTypeBindings_Table.TableEntityFunction)),
                b => ValidateTableInput(b));


            var enumerableTableEntityFunction = functions.Single(p => p.Name == "EnumerableTableEntityFunction");

            ValidateFunction(enumerableTableEntityFunction, "EnumerableTableEntityFunction", GetEntryPoint(nameof(SDKTypeBindings_Table), nameof(SDKTypeBindings_Table.EnumerableTableEntityFunction)),
                b => ValidateTableInput(b));


            var tableUnsupportedTypeFunction = functions.Single(p => p.Name == "TableUnsupportedTypeFunction");

            ValidateFunction(tableUnsupportedTypeFunction, "TableUnsupportedTypeFunction", GetEntryPoint(nameof(SDKTypeBindings_Table), nameof(SDKTypeBindings_Table.TableUnsupportedTypeFunction)),
                b => ValidateTableInputBypassDeferredBinding(b));


            var tablePocoFunction = functions.Single(p => p.Name == "TablePocoFunction");

            ValidateFunction(tablePocoFunction, "TablePocoFunction", GetEntryPoint(nameof(SDKTypeBindings_Table), nameof(SDKTypeBindings_Table.TablePocoFunction)),
                b => ValidateTableInputBypassDeferredBinding(b));

            void ValidateTableInput(ExpandoObject b)
            {
                AssertExpandoObject(b, new Dictionary<string, object>
                {
                    { "Name", "tableInput" },
                    { "Type", "table" },
                    { "Direction", "In" },
                    { "tableName", "tableName" },
                    { "Properties", new Dictionary<String, Object>( ) { { "SupportsDeferredBinding" , "True"} } }
                });
            }

            void ValidateTableInputBypassDeferredBinding(ExpandoObject b)
            {
                AssertExpandoObject(b, new Dictionary<string, object>
                {
                    { "Name", "tableInput" },
                    { "Type", "table" },
                    { "Direction", "In" },
                    { "tableName", "tableName" },
                    { "Properties", new Dictionary<String, Object>( ) { } }
                });
            }
        }

        [Fact]
        public void TimerFunction()
        {
            var generator = new FunctionMetadataGenerator();
            var module = ModuleDefinition.ReadModule(_thisAssembly.Location);
            var typeDef = TypeHelpers.GetTypeDefinition(typeof(Timer));
            var functions = generator.GenerateFunctionMetadata(typeDef);
            var extensions = generator.Extensions;

            ValidateFunction(functions.Single(), "TimerFunction", GetEntryPoint(nameof(Timer), nameof(Timer.RunTimer)),
                b => ValidateTrigger(b));

            AssertDictionary(extensions, new Dictionary<string, string>());

            void ValidateTrigger(ExpandoObject b)
            {
                AssertExpandoObject(b, new Dictionary<string, object>
                {
                    { "Name", "timer" },
                    { "Type", "timerTrigger" },
                    { "Direction", "In" },
                    { "schedule", "0 0 0 * * *" },
                    { "RunOnStartup", false },
                    { "Properties", new Dictionary<String, Object>() }
                });
            }
        }

        [Fact]
        public void QueueStorageFunctions_SDKTypeBindings()
        {
            var generator = new FunctionMetadataGenerator();
            var module = ModuleDefinition.ReadModule(_thisAssembly.Location);
            var typeDef = TypeHelpers.GetTypeDefinition(typeof(SDKTypeBindings_Queue));
            var functions = generator.GenerateFunctionMetadata(typeDef);
            var extensions = generator.Extensions;

            Assert.Equal(2, functions.Count());

            AssertDictionary(extensions, new Dictionary<string, string>
            {
                { "Microsoft.Azure.WebJobs.Extensions.Storage.Queues", "5.3.1" },
            });

            var queueMessageTriggerFunction = functions.Single(p => p.Name == nameof(SDKTypeBindings_Queue.QueueMessageTrigger));

            ValidateFunction(queueMessageTriggerFunction, nameof(SDKTypeBindings_Queue.QueueMessageTrigger), GetEntryPoint(nameof(SDKTypeBindings_Queue), nameof(SDKTypeBindings_Queue.QueueMessageTrigger)),
                ValidateQueueMessageTrigger);

            var queueBinaryDataTriggerFunction = functions.Single(p => p.Name == nameof(SDKTypeBindings_Queue.QueueBinaryDataTrigger));

            ValidateFunction(queueBinaryDataTriggerFunction, nameof(SDKTypeBindings_Queue.QueueBinaryDataTrigger), GetEntryPoint(nameof(SDKTypeBindings_Queue), nameof(SDKTypeBindings_Queue.QueueBinaryDataTrigger)),
                ValidateQueueBinaryDataTrigger);

            void ValidateQueueMessageTrigger(ExpandoObject b)
            {
                AssertExpandoObject(b, new Dictionary<string, object>
                {
                    { "Name", "message" },
                    { "Type", "queueTrigger" },
                    { "Direction", "In" },
                    { "queueName", "queue" },
                    { "Properties", new Dictionary<String, Object>( ) { { "SupportsDeferredBinding" , "True"} } }
                });
            }

            void ValidateQueueBinaryDataTrigger(ExpandoObject b)
            {
                AssertExpandoObject(b, new Dictionary<string, object>
                {
                    { "Name", "message" },
                    { "Type", "queueTrigger" },
                    { "Direction", "In" },
                    { "queueName", "queue" },
                    { "Properties", new Dictionary<String, Object>( ) { { "SupportsDeferredBinding" , "True"} } }
                });
            }
        }

        [Fact]
        public void MultiOutput_OnReturnType()
        {
            var generator = new FunctionMetadataGenerator();
            var module = ModuleDefinition.ReadModule(_thisAssembly.Location);
            var typeDef = TypeHelpers.GetTypeDefinition(typeof(MultiOutput_ReturnType));
            var functions = generator.GenerateFunctionMetadata(typeDef);
            var extensions = generator.Extensions;

            Assert.Single(functions);

            var queueToBlob = functions.Single(p => p.Name == "QueueToBlobFunction");

            ValidateFunction(queueToBlob, "QueueToBlobFunction", GetEntryPoint(nameof(MultiOutput_ReturnType), nameof(MultiOutput_ReturnType.QueueToBlob)),
                b => ValidateQueueTrigger(b),
                b => ValidateBlobOutput(b),
                b => ValidateQueueOutput(b));

            AssertDictionary(extensions, new Dictionary<string, string>
            {
                { "Microsoft.Azure.WebJobs.Extensions.Storage.Queues", "5.3.1" },
                { "Microsoft.Azure.WebJobs.Extensions.Storage.Blobs", "5.3.1" },
            });

            void ValidateQueueTrigger(ExpandoObject b)
            {
                AssertExpandoObject(b, new Dictionary<string, object>
                {
                    { "Name", "queuePayload" },
                    { "Type", "queueTrigger" },
                    { "Direction", "In" },
                    { "Connection", "MyConnection" },
                    { "queueName", "queueName" },
                    { "DataType", "String" },
                    { "Properties", new Dictionary<String, Object>() }
                });
            }

            void ValidateBlobOutput(ExpandoObject b)
            {
                AssertExpandoObject(b, new Dictionary<string, object>
                {
                    { "Name", "blobOutput" },
                    { "Type", "blob" },
                    { "Direction", "Out" },
                    { "blobPath", "container1/hello.txt" },
                    { "Connection", "MyOtherConnection" },
                    { "DataType", "String" },
                    { "Properties", new Dictionary<String, Object>() }
                });
            }

            void ValidateQueueOutput(ExpandoObject b)
            {
                AssertExpandoObject(b, new Dictionary<string, object>
                {
                    { "Name", "queueOutput" },
                    { "Type", "queue" },
                    { "Direction", "Out" },
                    { "queueName", "queue2" },
                    { "DataType", "String" },
                    { "Properties", new Dictionary<String, Object>() }
                });
            }
        }

        [Fact]
        public void MultiOutput_OnReturnType_WithHttp()
        {
            var generator = new FunctionMetadataGenerator();
            var module = ModuleDefinition.ReadModule(_thisAssembly.Location);
            var typeDef = TypeHelpers.GetTypeDefinition(typeof(MultiOutput_ReturnType_Http));
            var functions = generator.GenerateFunctionMetadata(typeDef);
            var extensions = generator.Extensions;

            Assert.Single(functions);

            var HttpAndQueue = functions.Single(p => p.Name == "HttpAndQueue");

            ValidateFunction(HttpAndQueue, "HttpAndQueue", GetEntryPoint(nameof(MultiOutput_ReturnType_Http), nameof(MultiOutput_ReturnType_Http.HttpAndQueue)),
                b => ValidateHttpTrigger(b),
                b => ValidateQueueOutput(b),
                b => ValidateHttpOutput(b));

            AssertDictionary(extensions, new Dictionary<string, string>
            {
                { "Microsoft.Azure.WebJobs.Extensions.Storage.Queues", "5.3.1" }
            });

            void ValidateHttpTrigger(ExpandoObject b)
            {
                AssertExpandoObject(b, new Dictionary<string, object>
                {
                    { "Name", "req" },
                    { "Type", "httpTrigger" },
                    { "Direction", "In" },
                    { "methods", new[] { "get" } },
                    { "DataType", "String" },
                    { "Properties", new Dictionary<String, Object>() }
                });
            }

            void ValidateHttpOutput(ExpandoObject b)
            {
                AssertExpandoObject(b, new Dictionary<string, object>
                {
                    { "Name", "httpResponseProp" },
                    { "Type", "http" },
                    { "Direction", "Out" }
                });
            }

            void ValidateQueueOutput(ExpandoObject b)
            {
                AssertExpandoObject(b, new Dictionary<string, object>
                {
                    { "Name", "queueOutput" },
                    { "Type", "queue" },
                    { "Direction", "Out" },
                    { "queueName", "queue2" },
                    { "DataType", "String" },
                    { "Properties", new Dictionary<String, Object>() }
                });
            }
        }

        [Fact]
        public void MultiOutput_OnReturnType_WithHttp_AspNet()
        {
            var generator = new FunctionMetadataGenerator();
            var module = ModuleDefinition.ReadModule(_thisAssembly.Location);
            var typeDef = TypeHelpers.GetTypeDefinition(typeof(MultiOutput_ReturnType_Http_AspNet));
            var functions = generator.GenerateFunctionMetadata(typeDef);
            var extensions = generator.Extensions;

            Assert.Single(functions);

            var HttpAndQueue = functions.Single(p => p.Name == "HttpAndQueue");

            ValidateFunction(HttpAndQueue, "HttpAndQueue", GetEntryPoint(nameof(MultiOutput_ReturnType_Http_AspNet), nameof(MultiOutput_ReturnType_Http_AspNet.HttpAndQueue)),
                b => ValidateHttpTrigger(b),
                b => ValidateQueueOutput(b),
                b => ValidateHttpOutput(b));

            AssertDictionary(extensions, new Dictionary<string, string>
            {
                { "Microsoft.Azure.WebJobs.Extensions.Storage.Queues", "5.3.1" }
            });

            void ValidateHttpTrigger(ExpandoObject b)
            {
                AssertExpandoObject(b, new Dictionary<string, object>
                {
                    { "Name", "req" },
                    { "Type", "httpTrigger" },
                    { "Direction", "In" },
                    { "methods", new[] { "get" } },
                    { "Properties", new Dictionary<String, Object>() }
                });
            }

            void ValidateHttpOutput(ExpandoObject b)
            {
                AssertExpandoObject(b, new Dictionary<string, object>
                {
                    { "Name", "httpResponseProp" },
                    { "Type", "http" },
                    { "Direction", "Out" }
                });
            }

            void ValidateQueueOutput(ExpandoObject b)
            {
                AssertExpandoObject(b, new Dictionary<string, object>
                {
                    { "Name", "queueOutput" },
                    { "Type", "queue" },
                    { "Direction", "Out" },
                    { "queueName", "queue2" },
                    { "DataType", "String" },
                    { "Properties", new Dictionary<String, Object>() }
                });
            }
        }

        [Fact]
        public void JustHttp_OnReturnTypeProperty()
        {
            var generator = new FunctionMetadataGenerator();
            var module = ModuleDefinition.ReadModule(_thisAssembly.Location);
            var typeDef = TypeHelpers.GetTypeDefinition(typeof(ReturnType_JustHttp));
            var functions = generator.GenerateFunctionMetadata(typeDef);
            var extensions = generator.Extensions;

            Assert.Single(functions);

            var HttpAndQueue = functions.Single(p => p.Name == "JustHttp");

            ValidateFunction(HttpAndQueue, "JustHttp", GetEntryPoint(nameof(ReturnType_JustHttp), nameof(ReturnType_JustHttp.JustHttp)),
                b => ValidateHttpTrigger(b),
                b => ValidateHttpOutput(b));

            AssertDictionary(extensions, new Dictionary<string, string>());

            void ValidateHttpTrigger(ExpandoObject b)
            {
                AssertExpandoObject(b, new Dictionary<string, object>
                {
                    { "Name", "req" },
                    { "Type", "httpTrigger" },
                    { "Direction", "In" },
                    { "methods", new[] { "get" } },
                    { "DataType", "String" },
                    { "Properties", new Dictionary<String, Object>() }
                });
            }

            void ValidateHttpOutput(ExpandoObject b)
            {
                AssertExpandoObject(b, new Dictionary<string, object>
                {
                    { "Name", "httpResponseProp" },
                    { "Type", "http" },
                    { "Direction", "Out" }
                });
            }
        }

        [Fact]
        public void MultiReturn_MultiAttribute_IsValid()
        {
            var generator = new FunctionMetadataGenerator();
            var module = ModuleDefinition.ReadModule(_thisAssembly.Location);
            var typeDef = TypeHelpers.GetTypeDefinition(typeof(MultiOutput_ReturnType_MultiAttribute));

            var functions = generator.GenerateFunctionMetadata(typeDef);
            var extensions = generator.Extensions;

            Assert.Single(functions);

            var HttpAndQueue = functions.Single(p => p.Name == "HttpAndQueue");

            ValidateFunction(HttpAndQueue, "HttpAndQueue", GetEntryPoint(nameof(MultiOutput_ReturnType_MultiAttribute), nameof(MultiOutput_ReturnType_MultiAttribute.HttpAndQueue)),
                b => ValidateHttpTrigger(b),
                b => ValidateQueueOutput(b),
                b => ValidateHttpOutput(b));

            AssertDictionary(extensions, new Dictionary<string, string>
            {
                { "Microsoft.Azure.WebJobs.Extensions.Storage.Queues", "5.3.1" }
            });

            void ValidateHttpTrigger(ExpandoObject b)
            {
                AssertExpandoObject(b, new Dictionary<string, object>
                {
                    { "Name", "req" },
                    { "Type", "httpTrigger" },
                    { "Direction", "In" },
                    { "methods", new[] { "get" } },
                    { "Properties", new Dictionary<String, Object>() }
                });
            }

            void ValidateHttpOutput(ExpandoObject b)
            {
                AssertExpandoObject(b, new Dictionary<string, object>
                {
                    { "Name", "httpResponseProp" },
                    { "Type", "http" },
                    { "Direction", "Out" }
                });
            }

            void ValidateQueueOutput(ExpandoObject b)
            {
                AssertExpandoObject(b, new Dictionary<string, object>
                {
                    { "Name", "queueOutput" },
                    { "Type", "queue" },
                    { "Direction", "Out" },
                    { "queueName", "queue2" },
                    { "DataType", "String" },
                    { "Properties", new Dictionary<String, Object>() }
                });
            }
        }

        [Fact]
        public void MultiOutput_OnMethod_Throws()
        {
            var generator = new FunctionMetadataGenerator();
            var module = ModuleDefinition.ReadModule(_thisAssembly.Location);
            var typeDef = TypeHelpers.GetTypeDefinition(typeof(MultiOutput_Method));

            var exception = Assert.Throws<FunctionsMetadataGenerationException>(() => generator.GenerateFunctionMetadata(typeDef));

            Assert.Contains($"Found multiple output bindings on method", exception.Message);
        }

        [Fact]
        public void MultiOutput_OnReturnTypeProperty_Throws()
        {
            var generator = new FunctionMetadataGenerator();
            var module = ModuleDefinition.ReadModule(_thisAssembly.Location);
            var typeDef = TypeHelpers.GetTypeDefinition(typeof(MultiReturn_MultiBinding));

            var exception = Assert.Throws<FunctionsMetadataGenerationException>(() => generator.GenerateFunctionMetadata(typeDef));

            Assert.Contains($"Found multiple output attributes on property", exception.Message);
        }

        [Theory]
        [InlineData("StringInputFunction", nameof(CardinalityMany.StringInputFunction), false, "String")]
        [InlineData("StringArrayInputFunction", nameof(CardinalityMany.StringArrayInputFunction), true, "String")]
        [InlineData("BinaryInputFunction", nameof(CardinalityMany.BinaryInputFunction), false, "Binary")]
        [InlineData("BinaryArrayInputFunction", nameof(CardinalityMany.BinaryArrayInputFunction), true, "Binary")]
        [InlineData("IntArrayInputFunction", nameof(CardinalityMany.IntArrayInputFunction), true, "")]
        [InlineData("StringListInputFunction", nameof(CardinalityMany.StringListInputFunction), true, "String")]
        [InlineData("BinaryListInputFunction", nameof(CardinalityMany.BinaryListInputFunction), true, "Binary")]
        [InlineData("EnumerableNestedStringGenericClassInputFunction", nameof(CardinalityMany.EnumerableNestedStringGenericClassInputFunction), true, "String")]
        [InlineData("EnumerableNestedStringGenericClass2InputFunction", nameof(CardinalityMany.EnumerableNestedStringGenericClass2InputFunction), true, "String")]
        [InlineData("IntListInputFunction", nameof(CardinalityMany.IntListInputFunction), true, "")]
        [InlineData("StringDoubleArrayInputFunction", nameof(CardinalityMany.StringDoubleArrayInputFunction), true, "")]
        [InlineData("EnumerableClassInputFunction", nameof(CardinalityMany.EnumerableClassInputFunction), true, "")]
        [InlineData("EnumerableStringClassInputFunction", nameof(CardinalityMany.EnumerableStringClassInputFunction), true, "String")]
        [InlineData("EnumerableBinaryClassInputFunction", nameof(CardinalityMany.EnumerableBinaryClassInputFunction), true, "Binary")]
        [InlineData("EnumerableGenericClassInputFunction", nameof(CardinalityMany.EnumerableGenericClassInputFunction), true, "")]
        [InlineData("EnumerableNestedBinaryClassInputFunction", nameof(CardinalityMany.EnumerableNestedBinaryClassInputFunction), true, "Binary")]
        [InlineData("EnumerableNestedStringClassInputFunction", nameof(CardinalityMany.EnumerableNestedStringClassInputFunction), true, "String")]
        [InlineData("LookupInputFunction", nameof(CardinalityMany.LookupInputFunction), false, "")]
        [InlineData("DictionaryInputFunction", nameof(CardinalityMany.DictionaryInputFunction), false, "")]
        [InlineData("ConcurrentDictionaryInputFunction", nameof(CardinalityMany.ConcurrentDictionaryInputFunction), false, "")]
        [InlineData("HashSetInputFunction", nameof(CardinalityMany.HashSetInputFunction), true, "String")]
        [InlineData("EnumerableInputFunction", nameof(CardinalityMany.EnumerableInputFunction), true, "")]
        [InlineData("EnumerableStringInputFunction", nameof(CardinalityMany.EnumerableStringInputFunction), true, "String")]
        [InlineData("EnumerableBinaryInputFunction", nameof(CardinalityMany.EnumerableBinaryInputFunction), true, "Binary")]
        [InlineData("EnumerableGenericInputFunction", nameof(CardinalityMany.EnumerableGenericInputFunction), true, "")]
        [InlineData("EnumerablePocoInputFunction", nameof(CardinalityMany.EnumerablePocoInputFunction), true, "")]
        [InlineData("ListPocoInputFunction", nameof(CardinalityMany.ListPocoInputFunction), true, "")]
        public void CardinalityManyFunctions(string functionName, string entryPoint, bool cardinalityMany, string dataType)
        {
            var generator = new FunctionMetadataGenerator();
            var typeDef = TypeHelpers.GetTypeDefinition(typeof(CardinalityMany));
            var functions = generator.GenerateFunctionMetadata(typeDef);
            var extensions = generator.Extensions;

            SdkFunctionMetadata metadata = functions.Single(a => string.Equals(a.Name, functionName, StringComparison.Ordinal));

            ValidateFunction(metadata, functionName, GetEntryPoint(nameof(CardinalityMany), entryPoint),
                b => ValidateTrigger(b, cardinalityMany));

            AssertDictionary(extensions, new Dictionary<string, string>(){
                { "Microsoft.Azure.WebJobs.Extensions.EventHubs", "6.3.5" }
            });

            void ValidateTrigger(ExpandoObject b, bool many)
            {
                var expected = new Dictionary<string, object>()
                {
                    { "Name", "input" },
                    { "Type", "eventHubTrigger" },
                    { "Direction", "In" },
                    { "eventHubName", "test" },
                    { "Connection", "EventHubConnectionAppSetting" },
                    { "Properties", new Dictionary<String, Object>() }
                };

                if (many)
                {
                    expected.Add("Cardinality", "Many");
                }
                else
                {
                    expected.Add("Cardinality", "One");
                }

                if (!string.IsNullOrEmpty(dataType))
                {
                    expected.Add("DataType", dataType);
                }

                AssertExpandoObject(b, expected);
            }
        }

        [Fact]
        public void CardinalityMany_WithNotIterableTypeThrows()
        {
            var generator = new FunctionMetadataGenerator();
            var typeDef = TypeHelpers.GetTypeDefinition(typeof(EventHubNotBatched));

            var exception = Assert.Throws<FunctionsMetadataGenerationException>(() => generator.GenerateFunctionMetadata(typeDef));
            Assert.Contains("Function is configured to process events in batches but parameter type is not iterable", exception.Message);
        }

        [Fact]
        public void EnableImplicitRegistration_NotSet()
        {
            // This test assembly explicitly has an ExtensionInformationAttribute, without setting EnableImplicitRegistration
            var generator = new FunctionMetadataGenerator();
            var module = ModuleDefinition.ReadModule(_thisAssembly.Location);

            generator.GenerateFunctionMetadata(module);
            Assert.Empty(generator.Extensions);
        }

        [Fact]
        public void EnableImplicitRegistration_True()
        {
            var generator = new FunctionMetadataGenerator();
            var module = ModuleDefinition.ReadModule(_thisAssembly.Location);

            // Inject enableImplicitRegistration = true into the constructor
            var enableImplicitRegistrationParam = new CustomAttributeArgument(TypeHelpers.GetTypeDefinition(typeof(bool)), true);
            var extInfo = module.Assembly.CustomAttributes.Single(p => p.AttributeType.FullName == Constants.ExtensionsInformationType);
            extInfo.ConstructorArguments.Add(enableImplicitRegistrationParam);

            generator.GenerateFunctionMetadata(module);
            var extension = generator.Extensions.Single();

            Assert.Equal("Sdk.Generator.Tests", extension.Key);
            Assert.Equal("1.0.0", extension.Value);
        }

        [Fact]
        public void EnableImplicitRegistration_False()
        {
            var generator = new FunctionMetadataGenerator();
            var module = ModuleDefinition.ReadModule(_thisAssembly.Location);

            // Inject enableImplicitRegistration = false into the constructor
            var enableImplicitRegistrationParam = new CustomAttributeArgument(TypeHelpers.GetTypeDefinition(typeof(bool)), false);
            var extInfo = module.Assembly.CustomAttributes.Single(p => p.AttributeType.FullName == Constants.ExtensionsInformationType);
            extInfo.ConstructorArguments.Add(enableImplicitRegistrationParam);

            generator.GenerateFunctionMetadata(module);
            Assert.Empty(generator.Extensions);
        }

        [Fact]
        public void FunctionWithNoRetryHasNullRetryProperty()
        {
            var generator = new FunctionMetadataGenerator();
            var module = ModuleDefinition.ReadModule(_thisAssembly.Location);
            var typeDef = TypeHelpers.GetTypeDefinition(typeof(Storage));
            var functions = generator.GenerateFunctionMetadata(typeDef);

            var queueToBlob = functions.Single(p => p.Name == "QueueToBlobFunction");

            Assert.Null(queueToBlob.Retry);
        }

        [Fact]
        public void FunctionWithFixedDelayRetry()
        {
            var generator = new FunctionMetadataGenerator();
            var module = ModuleDefinition.ReadModule(_thisAssembly.Location);
            var typeDef = TypeHelpers.GetTypeDefinition(typeof(RetryFunctions));

            var functions = generator.GenerateFunctionMetadata(typeDef);

            var funcName = "FixedDelayRetryFunction";
            var fixedDelayFunction = functions.Single(p => p.Name == funcName);
            var retry = fixedDelayFunction.Retry!;

            Assert.Equal("fixedDelay", retry.Strategy);
            Assert.Equal(5, retry.MaxRetryCount);
            Assert.Equal("00:00:10", retry.DelayInterval);
            Assert.Null(retry.MinimumInterval);
            Assert.Null(retry.MaximumInterval);

            FunctionMetadataJsonWriter.WriteMetadata(functions, ".");
        }

        [Fact]
        public void FunctionWithExponentialBackoffRetry()
        {
            var generator = new FunctionMetadataGenerator();
            var module = ModuleDefinition.ReadModule(_thisAssembly.Location);
            var typeDef = TypeHelpers.GetTypeDefinition(typeof(RetryFunctions));

            var functions = generator.GenerateFunctionMetadata(typeDef);

            var funcName = "ExponentialBackoffRetryFunction";
            var fixedDelayFunction = functions.Single(p => p.Name == funcName);
            var retry = fixedDelayFunction.Retry!;

            Assert.Equal("exponentialBackoff", retry.Strategy);
            Assert.Equal(5, retry.MaxRetryCount);
            Assert.Null(retry.DelayInterval);
            Assert.Equal("00:00:04", retry.MinimumInterval);
            Assert.Equal("00:15:00", retry.MaximumInterval);

            FunctionMetadataJsonWriter.WriteMetadata(functions, ".");
        }

        [Fact]
        public void FunctionWithRetryPolicyWithInvalidIntervals()
        {
            // negative intervals
            Assert.Throws<ArgumentOutOfRangeException>(() => new FixedDelayRetryAttribute(5, "-00:00:10"));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ExponentialBackoffRetryAttribute(5, "-00:00:14", "00:15:00"));

            // min interval greater than max interval
            Assert.Throws<ArgumentException>(() => new ExponentialBackoffRetryAttribute(5, "00:15:00", "00:00:14"));

            // invalid interval that can't be parsed
            Assert.Throws<ArgumentOutOfRangeException>(() => new FixedDelayRetryAttribute(5, "something_bad"));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ExponentialBackoffRetryAttribute(5, "something_bad", "00:01:00"));
        }

        [Fact]
        public void ServiceBus_SDKTypeBindings()
        {
            var generator = new FunctionMetadataGenerator();
            var module = ModuleDefinition.ReadModule(_thisAssembly.Location);
            var typeDef = TypeHelpers.GetTypeDefinition(typeof(SDKTypeBindings_ServiceBus));
            var functions = generator.GenerateFunctionMetadata(typeDef);
            var extensions = generator.Extensions;

            Assert.Equal(2, functions.Count());

            var extensionReference = typeof(ServiceBusExtensionStartup).Assembly.GetCustomAttribute<ExtensionInformationAttribute>()!;

            AssertDictionary(extensions, new Dictionary<string, string>
            {
                { "Microsoft.Azure.WebJobs.Extensions.ServiceBus", extensionReference.ExtensionVersion },
            });

            var serviceBusTriggerFunction = functions.Single(p => p.Name == nameof(SDKTypeBindings_ServiceBus.ServiceBusTriggerFunction));

            ValidateFunction(serviceBusTriggerFunction, nameof(SDKTypeBindings_ServiceBus.ServiceBusTriggerFunction), GetEntryPoint(nameof(SDKTypeBindings_ServiceBus), nameof(SDKTypeBindings_ServiceBus.ServiceBusTriggerFunction)),
                ValidateServiceBusTrigger);

            var serviceBusBatchTriggerFunction = functions.Single(p => p.Name == nameof(SDKTypeBindings_ServiceBus.ServiceBusBatchTriggerFunction));

            ValidateFunction(serviceBusBatchTriggerFunction, nameof(SDKTypeBindings_ServiceBus.ServiceBusBatchTriggerFunction), GetEntryPoint(nameof(SDKTypeBindings_ServiceBus), nameof(SDKTypeBindings_ServiceBus.ServiceBusBatchTriggerFunction)),
                ValidateServiceBusBatchTrigger);

            void ValidateServiceBusTrigger(ExpandoObject b)
            {
                AssertExpandoObject(b, new Dictionary<string, object>
                {
                    { "Name", "message" },
                    { "Type", "serviceBusTrigger" },
                    { "Direction", "In" },
                    { "queueName", "queue" },
                    { "Cardinality", "One" },
                    { "Properties", new Dictionary<String, Object>( ) { { "SupportsDeferredBinding" , "True"} } }
                });
            }

            void ValidateServiceBusBatchTrigger(ExpandoObject b)
            {
                AssertExpandoObject(b, new Dictionary<string, object>
                {
                    { "Name", "messages" },
                    { "Type", "serviceBusTrigger" },
                    { "Direction", "In" },
                    { "queueName", "queue" },
                    { "Cardinality", "Many" },
                    { "Properties", new Dictionary<String, Object>( ) { { "SupportsDeferredBinding" , "True"} } }
                });
            }
        }

        [Fact]
        public void EventHubs_SDKTypeBindings()
        {
            var generator = new FunctionMetadataGenerator();
            var module = ModuleDefinition.ReadModule(_thisAssembly.Location);
            var typeDef = TypeHelpers.GetTypeDefinition(typeof(SDKTypeBindings_EventHubs));
            var functions = generator.GenerateFunctionMetadata(typeDef);
            var extensions = generator.Extensions;

            Assert.Equal(2, functions.Count());

            AssertDictionary(extensions, new Dictionary<string, string>
            {
                { "Microsoft.Azure.WebJobs.Extensions.EventHubs", "6.3.5" },
            });

            var eventHubTriggerFunction = functions.Single(p => p.Name == nameof(SDKTypeBindings_EventHubs.EventHubTriggerFunction));

            ValidateFunction(eventHubTriggerFunction, nameof(SDKTypeBindings_EventHubs.EventHubTriggerFunction), GetEntryPoint(nameof(SDKTypeBindings_EventHubs), nameof(SDKTypeBindings_EventHubs.EventHubTriggerFunction)),
                ValidateEventHubTrigger);

            var eventHubBatchTriggerFunction = functions.Single(p => p.Name == nameof(SDKTypeBindings_EventHubs.EventHubBatchTriggerFunction));

            ValidateFunction(eventHubBatchTriggerFunction, nameof(SDKTypeBindings_EventHubs.EventHubBatchTriggerFunction), GetEntryPoint(nameof(SDKTypeBindings_EventHubs), nameof(SDKTypeBindings_EventHubs.EventHubBatchTriggerFunction)),
                ValidateEventHubBatchTrigger);

            void ValidateEventHubTrigger(ExpandoObject b)
            {
                AssertExpandoObject(b, new Dictionary<string, object>
                {
                    { "Name", "event" },
                    { "Type", "eventHubTrigger" },
                    { "Direction", "In" },
                    { "eventHubName", "hub" },
                    { "Cardinality", "One" },
                    { "Properties", new Dictionary<string, object>( ) { { "SupportsDeferredBinding" , "True"} } }
                });
            }

            void ValidateEventHubBatchTrigger(ExpandoObject b)
            {
                AssertExpandoObject(b, new Dictionary<string, object>
                {
                    { "Name", "events" },
                    { "Type", "eventHubTrigger" },
                    { "Direction", "In" },
                    { "eventHubName", "hub" },
                    { "Cardinality", "Many" },
                    { "Properties", new Dictionary<string, object>( ) { { "SupportsDeferredBinding" , "True"} } }
                });
            }
        }

        private class EventHubNotBatched
        {
            [Function("EventHubTrigger")]
            public static void EventHubTrigger([EventHubTrigger("test", Connection = "EventHubConnectionAppSetting")] string input,
                FunctionContext context)
            {
                throw new NotImplementedException();
            }
        }

        private static string GetEntryPoint(string className, string methodName) => $"{typeof(FunctionMetadataGeneratorTests).FullName}+{className}.{methodName}";

        private void ValidateFunction(SdkFunctionMetadata sdkFunctionMetadata, string name, string entryPoint, params Action<ExpandoObject>[] bindingValidations)
        {
            Assert.Equal(name, sdkFunctionMetadata.Name);
            Assert.Equal($"{_thisAssembly.GetName().Name}.dll", sdkFunctionMetadata.ScriptFile);
            Assert.Equal("dotnet-isolated", sdkFunctionMetadata.Language);
            Assert.Equal(sdkFunctionMetadata.EntryPoint, entryPoint);
            Assert.Null(sdkFunctionMetadata.FunctionDirectory);

            Assert.Single(sdkFunctionMetadata.Properties, p => p.Key == "IsCodeless" && p.Value.ToString() == "False");

            Assert.Collection(sdkFunctionMetadata.Bindings, bindingValidations);
        }

        private static void AssertExpandoObject(ExpandoObject expando, IDictionary<string, object> expected)
        {
            var dict = (IDictionary<string, object>)expando!;

            AssertDictionary(dict, expected);
        }

        private static void AssertDictionary<K, V>(IDictionary<K, V> dict, IDictionary<K, V> expected)
        {
            Assert.Equal(expected.Count, dict.Count);

            foreach (var kvp in expected)
            {
                Assert.Equal(kvp.Value, dict[kvp.Key]);
            }
        }

        private class BasicHttp
        {
            public const string FunctionName = "BasicHttpFunction";

            [Function(FunctionName)]
            public Task<HttpResponseData> Http([HttpTrigger(AuthorizationLevel.Admin, "get", "Post", Route = "/api2")] HttpRequestData myReq)
            {
                throw new NotImplementedException();
            }
        }

        private class BasicHttpAspNet
        {
            public const string FunctionName = "BasicHttpAspNetFunction";

            [Function(FunctionName)]
            public IActionResult Http([HttpTrigger(AuthorizationLevel.Admin, "get", "Post", Route = "/api2")] HttpRequest myReq)
            {
                throw new NotImplementedException();
            }
        }

        private class BasicHttpWithNoResponse
        {
            public const string FunctionName = "BasicHttpWithNoResponse";

            [Function(FunctionName)]
            public void Http([HttpTrigger(AuthorizationLevel.Admin, "get", "Post", Route = "/api2")] HttpRequestData myReq)
            {
                throw new NotImplementedException();
            }
        }

        private class Storage
        {
            public void AnotherMethod()
            {
            }

            [Function("QueueToBlobFunction")]
            [BlobOutput("container1/hello.txt", Connection = "MyOtherConnection")]
            public string QueueToBlob(
                [QueueTrigger("queueName", Connection = "MyConnection")] string queuePayload)
            {
                throw new NotImplementedException();
            }

            [Function("BlobToQueueFunction")]
            [QueueOutput("queue2")]
            public object BlobToQueue(
                [BlobTrigger("container2/%file%")] string blob)

            {
                throw new NotImplementedException();
            }

            [Function("BlobToBlobsFunction")]
            [BlobOutput("container1/hello.txt", Connection = "MyOtherConnection")]
            public object BlobToBlobs(
                [BlobTrigger("container2/%file%")] string blob,
                [BlobInput("container2")] IEnumerable<string> blobinput)
            {
                throw new NotImplementedException();
            }
        }

        private class SDKTypeBindings_BlobStorage
        {
            [Function("BlobStringToBlobStringFunction")]
            [BlobOutput("container1/hello.txt", Connection = "MyOtherConnection")]
            public object BlobStringToBlobStringFunction(
                [BlobTrigger("container2/%file%")] string blob,
                [BlobInput("container2/%file%")] string blobinput)
            {
                throw new NotImplementedException();
            }

            [Function("BlobClientToBlobStringFunction")]
            [BlobOutput("container1/hello.txt", Connection = "MyOtherConnection")]
            public object BlobClientToBlobStreamFunction(
                [BlobTrigger("container2/%file%")] BlobClient blob,
                [BlobInput("container2/%file%")] Stream blobinput)
            {
                throw new NotImplementedException();
            }

            [Function("BlobByteArrayToBlobByteArrayFunction")]
            [BlobOutput("container1/hello.txt", Connection = "MyOtherConnection")]
            public object BlobByteArrayToBlobByteArrayFunction(
                [BlobTrigger("container2/%file%")] byte[] blob,
                [BlobInput("container2/%file%")] byte[] blobinput)
            {
                throw new NotImplementedException();
            }

            [Function("BlobUnsupportedTypeToBlobStringFunction")]
            [BlobOutput("container1/hello.txt", Connection = "MyOtherConnection")]
            public object BlobUnsupportedTypeToBlobClientFunction(
                [BlobTrigger("container2/%file%")] BinaryData blob,
                [BlobInput("container2/%file%")] BlobClient blobinput)
            {
                throw new NotImplementedException();
            }

            [Function("BlobPocoToBlobUnsupportedType")]
            [BlobOutput("container1/hello.txt", Connection = "MyOtherConnection")]
            public object BlobPocoToBlobUnsupportedType(
                [BlobTrigger("container2/%file%")] Poco blob,
                [BlobInput("container2/%file%")] BinaryData blobinput)
            {
                throw new NotImplementedException();
            }
        }

        private class SDKTypeBindings_Table
        {
            [Function("TableClientFunction")]
            public object TableClientFunction(
                [TableInput("tableName")] TableClient tableInput)
            {
                throw new NotImplementedException();
            }


            [Function("TableEntityFunction")]
            public object TableEntityFunction(
                [TableInput("tableName")] TableEntity tableInput)
            {
                throw new NotImplementedException();
            }


            [Function("EnumerableTableEntityFunction")]
            public object EnumerableTableEntityFunction(
                [TableInput("tableName")] IEnumerable<TableEntity> tableInput)
            {
                throw new NotImplementedException();
            }

            [Function("TableUnsupportedTypeFunction")]
            public object TableUnsupportedTypeFunction(
                [TableInput("tableName")] BinaryData tableInput)
            {
                throw new NotImplementedException();
            }

            [Function("TablePocoFunction")]
            public object TablePocoFunction(
                [TableInput("tableName")] Poco tableInput)
            {
                throw new NotImplementedException();
            }
        }

        private class SDKTypeBindings_BlobCollection
        {
            [Function("BlobStringToBlobStringArray")]
            [BlobOutput("container1/hello.txt", Connection = "MyOtherConnection")]
            public object BlobStringToBlobStringArray(
                [BlobTrigger("container2/%file%")] string blob,
                [BlobInput("container2")] string[] blobinput)
            {
                throw new NotImplementedException();
            }


            [Function("BlobStringToBlobClientEnumerable")]
            [BlobOutput("container1/hello.txt", Connection = "MyOtherConnection")]
            public object BlobStringToBlobClientEnumerable(
                [BlobTrigger("container2/%file%")] string blob,
                [BlobInput("container2")] IEnumerable<BlobClient> blobinput)
            {
                throw new NotImplementedException();
            }

            [Function("BlobStringToBlobPocoEnumerable")]
            [BlobOutput("container1/hello.txt", Connection = "MyOtherConnection")]
            public object BlobStringToBlobPocoEnumerable(
                [BlobTrigger("container2/%file%")] string blob,
                [BlobInput("container2")] IEnumerable<Poco> blobinput)
            {
                throw new NotImplementedException();
            }

            [Function("BlobStringToBlobPocoArray")]
            [BlobOutput("container1/hello.txt", Connection = "MyOtherConnection")]
            public object BlobStringToBlobPocoArray(
                [BlobTrigger("container2/%file%")] string blob,
                [BlobInput("container2")] Poco[] blobinput)
            {
                throw new NotImplementedException();
            }
        }

        private class SDKTypeBindings_EventHubs
        {
            [Function(nameof(EventHubTriggerFunction))]
            public static void EventHubTriggerFunction(
                [EventHubTrigger("hub", IsBatched = false)] EventData @event)
            {
                throw new NotImplementedException();
            }

            [Function(nameof(EventHubBatchTriggerFunction))]
            public static void EventHubBatchTriggerFunction(
                [EventHubTrigger("hub")] EventData[] events)
            {
                throw new NotImplementedException();
            }
        }

        private class SDKTypeBindings_Queue
        {
            [Function(nameof(QueueMessageTrigger))]
            public static void QueueMessageTrigger(
                [QueueTrigger("queue")] QueueMessage message)
            {
                throw new NotImplementedException();
            }

            [Function(nameof(QueueBinaryDataTrigger))]
            public static void QueueBinaryDataTrigger(
                [QueueTrigger("queue")] BinaryData message)

            {
                throw new NotImplementedException();
            }
        }

        private class SDKTypeBindings_ServiceBus
        {
            [Function(nameof(ServiceBusTriggerFunction))]
            public static void ServiceBusTriggerFunction(
                [ServiceBusTrigger("queue")] ServiceBusReceivedMessage message)
            {
                throw new NotImplementedException();
            }

            [Function(nameof(ServiceBusBatchTriggerFunction))]
            public static void ServiceBusBatchTriggerFunction(
                [ServiceBusTrigger("queue", IsBatched = true)] ServiceBusReceivedMessage[] messages)
            {
                throw new NotImplementedException();
            }
        }

        private class ExternalType_Return
        {
            public const string FunctionName = "BasicHttpWithExternalTypeReturn";

            [Function(FunctionName)]
            public ExternalPoco Http([HttpTrigger(AuthorizationLevel.Admin, "get", "Post", Route = "/api2")] HttpRequestData myReq)
            {
                throw new NotImplementedException();
            }
        }

        private class MultiOutput_Method
        {
            [Function("QueueToBlobFunction")]
            [BlobOutput("container1/hello.txt", Connection = "MyOtherConnection")]
            [QueueOutput("queue2")]
            public string QueueToBlob(
                [QueueTrigger("queueName", Connection = "MyConnection")] string queuePayload)
            {
                throw new NotImplementedException();
            }
        }

        private class MultiReturn_MultiBinding
        {
            [Function("QueueToBlobFunction")]
            public MultiReturn_MultiBindingOnProp QueueToBlob(
    [QueueTrigger("queueName", Connection = "MyConnection")] string queuePayload)
            {
                throw new NotImplementedException();
            }
        }

        private class MultiOutput_ReturnType
        {
            [Function("QueueToBlobFunction")]
            public MultiReturn QueueToBlob(
                [QueueTrigger("queueName", Connection = "MyConnection")] string queuePayload)
            {
                throw new NotImplementedException();
            }
        }

        private class ReturnType_JustHttp
        {
            [Function("JustHttp")]
            public JustHttp JustHttp(
                [HttpTrigger("get")] string req)
            {
                throw new NotImplementedException();
            }
        }

        private class MultiOutput_ReturnType_Http
        {
            [Function("HttpAndQueue")]
            public MultiReturn_Http HttpAndQueue(
                [HttpTrigger("get")] string req)
            {
                throw new NotImplementedException();
            }
        }

        private class MultiOutput_ReturnType_Http_AspNet
        {
            [Function("HttpAndQueue")]
            public MultiReturn_Http_AspNet HttpAndQueue(
    [HttpTrigger("get")] HttpRequest req)
            {
                throw new NotImplementedException();
            }
        }

        private class MultiOutput_ReturnType_MultiAttribute
        {
            [Function("HttpAndQueue")]
            public MultiReturn_MultiAttribute HttpAndQueue(
[HttpTrigger("get")] HttpRequest req)
            {
                throw new NotImplementedException();
            }
        }

        private class MultiReturn
        {
            [BlobOutput("container1/hello.txt", Connection = "MyOtherConnection")]
            public string? blobOutput { get; set; }

            [QueueOutput("queue2")]
            public string? queueOutput { get; set; }
        }

        private class MultiReturn_Http
        {
            [QueueOutput("queue2")]
            public string? queueOutput { get; set; }

            public HttpResponseData? httpResponseProp { get; set; }
        }

        private class MultiReturn_Http_AspNet
        {
            [QueueOutput("queue2")]
            public string? queueOutput { get; set; }

            [HttpResult]
            public IActionResult? httpResponseProp { get; set; }
        }

        private class MultiReturn_MultiAttribute
        {
            [QueueOutput("queue2")]
            public string? queueOutput { get; set; }

            [SuppressMessage("Microsoft.Naming", "Foo", Justification = "Bar")]
            [HttpResult]
            [CustomTest]
            public IActionResult? httpResponseProp { get; set; }
        }

        private class CustomTestAttribute: Attribute
        {

        }

        private class MultiReturn_MultiBindingOnProp
        {
            [BlobOutput("./result")]
            [QueueOutput("queue2")]
            public string? queueOutput { get; set; }

            [HttpResult]
            public IActionResult? httpResponseProp { get; set; }
        }

        private class JustHttp
        {
            public HttpResponseData? httpResponseProp { get; set; }
        }

        private class Timer
        {
            [Function("TimerFunction")]
            public Task RunTimer([TimerTrigger("0 0 0 * * *", RunOnStartup = false)] object timer)
            {
                throw new NotImplementedException();
            }
        }

        private class CardinalityMany
        {
            [Function("StringInputFunction")]
            public static void StringInputFunction([EventHubTrigger("test", Connection = "EventHubConnectionAppSetting", IsBatched = false)] string input,
                FunctionContext context)
            {
                throw new NotImplementedException();
            }

            [Function("StringArrayInputFunction")]
            public static void StringArrayInputFunction([EventHubTrigger("test", Connection = "EventHubConnectionAppSetting")] string[] input,
                FunctionContext context)
            {
                throw new NotImplementedException();
            }

            [Function("BinaryInputFunction")]
            public static void BinaryInputFunction([EventHubTrigger("test", Connection = "EventHubConnectionAppSetting", IsBatched = false)] byte[] input,
                FunctionContext context)
            {
                throw new NotImplementedException();
            }

            [Function("BinaryArrayInputFunction")]
            public static void BinaryArrayInputFunction([EventHubTrigger("test", Connection = "EventHubConnectionAppSetting")] byte[][] input,
                FunctionContext context)
            {
                throw new NotImplementedException();
            }

            [Function("IntArrayInputFunction")]
            public static void IntArrayInputFunction([EventHubTrigger("test", Connection = "EventHubConnectionAppSetting")] int[] input,
                FunctionContext context)
            {
                throw new NotImplementedException();
            }

            [Function("StringListInputFunction")]
            public static void StringListInputFunction([EventHubTrigger("test", Connection = "EventHubConnectionAppSetting")] List<string> input,
                FunctionContext context)
            {
                throw new NotImplementedException();
            }

            [Function("StringDoubleArrayInputFunction")]
            public static void StringDoubleArrayInputFunction([EventHubTrigger("test", Connection = "EventHubConnectionAppSetting")] string[][] input,
                FunctionContext context)
            {
                throw new NotImplementedException();
            }

            [Function("BinaryListInputFunction")]
            public static void BinaryListInputFunction([EventHubTrigger("test", Connection = "EventHubConnectionAppSetting")] List<byte[]> input,
                FunctionContext context)
            {
                throw new NotImplementedException();
            }

            [Function("IntListInputFunction")]
            public static void IntListInputFunction([EventHubTrigger("test", Connection = "EventHubConnectionAppSetting")] int[] input,
                FunctionContext context)
            {
                throw new NotImplementedException();
            }

            [Function("EnumerableClassInputFunction")]
            public static void EnumerableClassInputFunction([EventHubTrigger("test", Connection = "EventHubConnectionAppSetting")] EnumerableTestClass input,
                FunctionContext context)
            {
                throw new NotImplementedException();
            }

            [Function("EnumerableStringClassInputFunction")]
            public static void EnumerableStringClassInputFunction([EventHubTrigger("test", Connection = "EventHubConnectionAppSetting")] EnumerableStringTestClass input,
                FunctionContext context)
            {
                throw new NotImplementedException();
            }

            [Function("EnumerableBinaryClassInputFunction")]
            public static void EnumerableBinaryClassInputFunction([EventHubTrigger("test", Connection = "EventHubConnectionAppSetting")] EnumerableBinaryTestClass input,
                FunctionContext context)
            {
                throw new NotImplementedException();
            }

            [Function("EnumerableGenericClassInputFunction")]
            public static void EnumerableGenericClassInputFunction([EventHubTrigger("test", Connection = "EventHubConnectionAppSetting")] EnumerableGenericTestClass input,
                FunctionContext context)
            {
                throw new NotImplementedException();
            }

            [Function("EnumerableNestedBinaryClassInputFunction")]
            public static void EnumerableNestedBinaryClassInputFunction([EventHubTrigger("test", Connection = "EventHubConnectionAppSetting")] EnumerableBinaryNestedTestClass input,
                FunctionContext context)
            {
                throw new NotImplementedException();
            }

            [Function("EnumerableNestedStringClassInputFunction")]
            public static void EnumerableNestedStringClassInputFunction([EventHubTrigger("test", Connection = "EventHubConnectionAppSetting")] EnumerableStringNestedTestClass input,
                FunctionContext context)
            {
                throw new NotImplementedException();
            }

            [Function("EnumerableNestedStringGenericClassInputFunction")]
            public static void EnumerableNestedStringGenericClassInputFunction([EventHubTrigger("test", Connection = "EventHubConnectionAppSetting")] EnumerableStringNestedGenericTestClass<string> input,
                FunctionContext context)
            {
                throw new NotImplementedException();
            }

            [Function("EnumerableNestedStringGenericClass2InputFunction")]
            public static void EnumerableNestedStringGenericClass2InputFunction([EventHubTrigger("test", Connection = "EventHubConnectionAppSetting")] EnumerableStringNestedGenericTestClass2<string, int> input,
                FunctionContext context)
            {
                throw new NotImplementedException();
            }

            [Function("LookupInputFunction")]
            public static void LookupInputFunction([EventHubTrigger("test", Connection = "EventHubConnectionAppSetting", IsBatched = false)] Lookup<string, int> input,
                FunctionContext context)
            {
                throw new NotImplementedException();
            }

            [Function("DictionaryInputFunction")]
            public static void DictionaryInputFunction([EventHubTrigger("test", Connection = "EventHubConnectionAppSetting", IsBatched = false)] Dictionary<string, string> input,
                FunctionContext context)
            {
                throw new NotImplementedException();
            }

            [Function("ConcurrentDictionaryInputFunction")]
            public static void ConcurrentDictionaryInputFunction([EventHubTrigger("test", Connection = "EventHubConnectionAppSetting", IsBatched = false)] ConcurrentDictionary<string, string> input,
                FunctionContext context)
            {
                throw new NotImplementedException();
            }

            [Function("HashSetInputFunction")]
            public static void HashSetInputFunction([EventHubTrigger("test", Connection = "EventHubConnectionAppSetting")] HashSet<string> input,
                FunctionContext context)
            {
                throw new NotImplementedException();
            }

            [Function("EnumerableInputFunction")]
            public static void EnumerableInputFunction([EventHubTrigger("test", Connection = "EventHubConnectionAppSetting")] IEnumerable input,
                FunctionContext context)
            {
                throw new NotImplementedException();
            }

            [Function("EnumerableStringInputFunction")]
            public static void EnumerableStringInputFunction([EventHubTrigger("test", Connection = "EventHubConnectionAppSetting")] IEnumerable<string> input,
                FunctionContext context)
            {
                throw new NotImplementedException();
            }

            [Function("EnumerableBinaryInputFunction")]
            public static void EnumerableBinaryInputFunction([EventHubTrigger("test", Connection = "EventHubConnectionAppSetting")] IEnumerable<byte[]> input,
                FunctionContext context)
            {
                throw new NotImplementedException();
            }

            [Function("EnumerableGenericInputFunction")]
            public static void EnumerableGenericInputFunction<T>([EventHubTrigger("test", Connection = "EventHubConnectionAppSetting")] IEnumerable<T> input,
                FunctionContext context)
            {
                throw new NotImplementedException();
            }

            [Function("EnumerablePocoInputFunction")]
            public static void EnumerablePocoInputFunction<T>([EventHubTrigger("test", Connection = "EventHubConnectionAppSetting")] IEnumerable<Poco> input,
                FunctionContext context)
            {
                throw new NotImplementedException();
            }

            [Function("ListPocoInputFunction")]
            public static void ListPocoInputFunction<T>([EventHubTrigger("test", Connection = "EventHubConnectionAppSetting")] List<Poco> input,
                FunctionContext context)
            {
                throw new NotImplementedException();
            }
        }
        private class RetryFunctions
        {
            [Function("FixedDelayRetryFunction")]
            [FixedDelayRetry(5, "00:00:10")]
            public void FixedDelayRetryFunction([QueueTrigger("queueName", Connection = "MyConnection")] string queuePayload)
            {
                throw new NotImplementedException();
            }

            [Function("ExponentialBackoffRetryFunction")]
            [ExponentialBackoffRetry(5, "00:00:04", "00:15:00")]
            public void ExponentialBackoffRetryFunction([QueueTrigger("queueName", Connection = "MyConnection")] string queuePayload)
            {
                throw new NotImplementedException();
            }
        }

        private class EnumerableTestClass : IEnumerable
        {
            public IEnumerator GetEnumerator()
            {
                throw new NotImplementedException();
            }
        }

        private class EnumerableStringTestClass : IEnumerable<string>
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

        private class EnumerableBinaryTestClass : IEnumerable<byte[]>
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

        private class EnumerableStringTestClass<T> : List<T>
        {
        }

        private class EnumerableBinaryNestedTestClass : EnumerableBinaryTestClass
        {
        }

        private class EnumerableStringNestedTestClass : EnumerableStringTestClass
        {
        }

        private class EnumerableStringNestedGenericTestClass2<T, V> : EnumerableStringNestedGenericTestClass<T>
        {
        }

        private class EnumerableStringNestedGenericTestClass<TK> : EnumerableStringTestClass<TK>
        {
        }

        private class EnumerableGenericTestClass : IEnumerable<int>
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

        private class Poco
        {
        }
    }
}
