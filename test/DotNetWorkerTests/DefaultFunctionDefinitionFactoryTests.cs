// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.IO;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Invocation;
using Microsoft.Azure.Functions.Worker.OutputBindings;
using Xunit;
using static Microsoft.Azure.Functions.Worker.Grpc.Messages.BindingInfo.Types;

namespace Microsoft.Azure.Functions.Worker.Tests
{
    public class DefaultFunctionDefinitionFactoryTests
    {
        [Fact]
        public void Creates()
        {
            var bindingInfoProvider = new DefaultOutputBindingsInfoProvider();
            var factory = new DefaultFunctionDefinitionFactory(bindingInfoProvider, new DefaultMethodInfoLocator());

            string fullPathToThisAssembly = GetType().Assembly.Location;
            var request = new FunctionLoadRequest
            {
                FunctionId = "abc",
                Metadata = new RpcFunctionMetadata
                {
                    EntryPoint = $"Microsoft.Azure.Functions.Worker.Tests.{nameof(DefaultFunctionDefinitionFactoryTests)}+{nameof(MyFunctionClass)}.{nameof(MyFunctionClass.Run)}",
                    ScriptFile = Path.GetFileName(fullPathToThisAssembly),
                    Name = "myfunction"
                }
            };

            // We base this on the request exclusively, not the binding attributes.
            request.Metadata.Bindings.Add("req", new BindingInfo { Type = "HttpTrigger", Direction = Direction.In });
            request.Metadata.Bindings.Add("$return", new BindingInfo { Type = "Http", Direction = Direction.Out });

            FunctionDefinition definition = factory.Create(request);

            Assert.Equal(request.FunctionId, definition.Id);
            Assert.Equal(request.Metadata.EntryPoint, definition.EntryPoint);
            Assert.Equal(request.Metadata.Name, definition.Name);
            Assert.Equal(fullPathToThisAssembly, definition.PathToAssembly);

            Assert.IsType<MethodReturnOutputBindingsInfo>(definition.OutputBindingsInfo);

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
    }
}
