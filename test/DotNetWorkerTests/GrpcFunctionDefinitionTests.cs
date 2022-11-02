// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.IO;
using System.Threading;
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
    }
}
