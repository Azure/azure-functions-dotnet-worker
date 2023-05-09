// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Azure.Functions.Tests;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Core;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Invocation;
using Microsoft.Azure.Functions.Worker.OutputBindings;
using Microsoft.Azure.Functions.Worker.Rpc;
using Xunit;
using static Microsoft.Azure.Functions.Worker.Grpc.Messages.BindingInfo.Types;

namespace Microsoft.Azure.Functions.Worker.Tests
{
    public class GrpcFunctionDefinitionTests
    {
        [Fact]
        public void Creates()
        {
            using var testVariables = new TestScopedEnvironmentVariable("FUNCTIONS_WORKER_DIRECTORY", ".");

            var bindingInfoProvider = new DefaultOutputBindingsInfoProvider();
            var methodInfoLocator = new DefaultMethodInfoLocator();

            string fullPathToThisAssembly = GetType().Assembly.Location;
            var functionLoadRequest = new FunctionLoadRequest
            {
                FunctionId = "abc",
                Metadata = new RpcFunctionMetadata
                {
                    EntryPoint = $"Microsoft.Azure.Functions.Worker.Tests.{nameof(GrpcFunctionDefinitionTests)}+{nameof(MyFunctionClass)}.{nameof(MyFunctionClass.Run)}",
                    ScriptFile = Path.GetFileName(fullPathToThisAssembly),
                    Name = "myfunction"
                }
            };

            // We base this on the request exclusively, not the binding attributes.
            functionLoadRequest.Metadata.Bindings.Add("req", new BindingInfo { Type = "HttpTrigger", Direction = Direction.In });
            functionLoadRequest.Metadata.Bindings.Add("$return", new BindingInfo { Type = "Http", Direction = Direction.Out });

            FunctionDefinition definition = functionLoadRequest.ToFunctionDefinition(methodInfoLocator);

            Assert.Equal(functionLoadRequest.FunctionId, definition.Id);
            Assert.Equal(functionLoadRequest.Metadata.EntryPoint, definition.EntryPoint);
            Assert.Equal(functionLoadRequest.Metadata.Name, definition.Name);
            Assert.Equal(fullPathToThisAssembly, definition.PathToAssembly);

            // Parameters
            Assert.Collection(definition.Parameters,
                p =>
                {
                    Assert.Equal("req", p.Name);
                    Assert.Equal(typeof(HttpRequestData), p.Type);
                });

            // InputBindings
            Assert.Collection(definition.InputBindings,
                p =>
                {
                    Assert.Equal("req", p.Key);
                    Assert.Equal(BindingDirection.In, p.Value.Direction);
                    Assert.Equal("HttpTrigger", p.Value.Type);
                });

            // OutputBindings
            Assert.Collection(definition.OutputBindings,
                p =>
                {
                    Assert.Equal("$return", p.Key);
                    Assert.Equal(BindingDirection.Out, p.Value.Direction);
                    Assert.Equal("Http", p.Value.Type);
                });
        }

        [Fact]
        public void GrpcFunctionDefinition_BlobInput_Creates()
        {
            using var testVariables = new TestScopedEnvironmentVariable("FUNCTIONS_WORKER_DIRECTORY", ".");

            var bindingInfoProvider = new DefaultOutputBindingsInfoProvider();
            var methodInfoLocator = new DefaultMethodInfoLocator();

            string fullPathToThisAssembly = GetType().Assembly.Location;
            var functionLoadRequest = new FunctionLoadRequest
            {
                FunctionId = "abc",
                Metadata = new RpcFunctionMetadata
                {
                    EntryPoint = $"Microsoft.Azure.Functions.Worker.Tests.{nameof(GrpcFunctionDefinitionTests)}+{nameof(MyBlobFunctionClass)}.{nameof(MyBlobFunctionClass.Run)}",
                    ScriptFile = Path.GetFileName(fullPathToThisAssembly),
                    Name = "myfunction"
                }
            };

            // We base this on the request exclusively, not the binding attributes.
            functionLoadRequest.Metadata.Bindings.Add("req", new BindingInfo { Type = "HttpTrigger", Direction = Direction.In });
            functionLoadRequest.Metadata.Bindings.Add("$return", new BindingInfo { Type = "Http", Direction = Direction.Out });

            FunctionDefinition definition = functionLoadRequest.ToFunctionDefinition(methodInfoLocator);

            Assert.Equal(functionLoadRequest.FunctionId, definition.Id);
            Assert.Equal(functionLoadRequest.Metadata.EntryPoint, definition.EntryPoint);
            Assert.Equal(functionLoadRequest.Metadata.Name, definition.Name);
            Assert.Equal(fullPathToThisAssembly, definition.PathToAssembly);

            // Parameters
            Assert.Collection(definition.Parameters,
                p =>
                {
                    Assert.Equal("req", p.Name);
                    Assert.Equal(typeof(HttpRequestData), p.Type);
                },
                q =>
                {
                    Assert.Equal("myBlob", q.Name);
                    Assert.Equal(typeof(string), q.Type);
                    Assert.Contains(PropertyBagKeys.AllowConverterFallback, q.Properties.Keys);
                    Assert.Contains(PropertyBagKeys.BindingAttributeSupportedConverters, q.Properties.Keys);
                    Assert.True(true, q.Properties[PropertyBagKeys.AllowConverterFallback].ToString());
                    Assert.Contains(new Dictionary<Type, List<Type>>().ToString(), q.Properties[PropertyBagKeys.BindingAttributeSupportedConverters].ToString());
                });

            // InputBindings
            Assert.Collection(definition.InputBindings,
                p =>
                {
                    Assert.Equal("req", p.Key);
                    Assert.Equal(BindingDirection.In, p.Value.Direction);
                    Assert.Equal("HttpTrigger", p.Value.Type);
                });

            // OutputBindings
            Assert.Collection(definition.OutputBindings,
                p =>
                {
                    Assert.Equal("$return", p.Key);
                    Assert.Equal(BindingDirection.Out, p.Value.Direction);
                    Assert.Equal("Http", p.Value.Type);
                });
        }


        private class MyFunctionClass
        {
            public HttpResponseData Run(HttpRequestData req)
            {
                return req.CreateResponse();
            }
        }

        private class MyFunctionClassWithCancellation
        {
            public HttpResponseData Run(HttpRequestData req, CancellationToken cancellationToken)
            {
                return req.CreateResponse();
            }
        }

        private class MyBlobFunctionClass
        {
            public HttpResponseData Run(
                [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
                [BlobInput("input-container/{id}.txt")] string myBlob)
            {
                return req.CreateResponse();
            }
        }

    }
}
