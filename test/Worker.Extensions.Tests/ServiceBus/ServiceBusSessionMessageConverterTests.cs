// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker.Converters;
using static Microsoft.Azure.Functions.Worker.Extensions.Tests.ServiceBusMessageActionsTests;
using Microsoft.Azure.Functions.Worker.Tests.Converters;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Extensions.Tests
{
    public class ServiceBusSessionMessageConverterTests
    {
        internal sealed class TestBindingContext : BindingContext
        {
            public TestBindingContext(IReadOnlyDictionary<string, object> input)
            {
                BindingData = input;
            }

            public override IReadOnlyDictionary<string, object> BindingData { get; }
        }

        internal sealed class TestFunctionContext : FunctionContext
        {
            public TestFunctionContext(BindingContext bindingContext)
            {
                BindingContext = bindingContext;
            }

            public override BindingContext BindingContext { get; }

            public override string InvocationId => throw new NotImplementedException();

            public override string FunctionId => throw new NotImplementedException();

            public override TraceContext TraceContext => throw new NotImplementedException();

            public override RetryContext RetryContext => throw new NotImplementedException();

            public override IServiceProvider InstanceServices { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public override FunctionDefinition FunctionDefinition => throw new NotImplementedException();

            public override IDictionary<object, object> Items { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public override IInvocationFeatures Features => throw new NotImplementedException();
        }

        [Fact]
        public async Task ConvertAsync_ReturnsSuccess()
        {
            var data = "{\"SessionLockedUntil\":\"2024-12-05T21:10:36.1193094+00:00\"}";
            
            var bindingDataDictionary = new Dictionary<string, object>
            {
                { "SessionId", "test" },
                { "SessionActions", JsonSerializer.Serialize(new
                    {
                        SessionLockedUntil = "2024-12-05T21:10:36.1193094+00:00"
                    })
                }
            };
            var context = new TestConverterContext(typeof(ServiceBusSessionMessageActions), data, new TestFunctionContext(new TestBindingContext(bindingDataDictionary)));
            var converter = new ServiceBusSessionMessageActionsConverter(new MockSettlementClient("test"));
            var result = await converter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Succeeded, result.Status);
            var output = result.Value as ServiceBusSessionMessageActions;
            Assert.NotNull(output);
        }

        [Fact]
        public async Task ConvertAsync_Batch_ForReceivedMessage_ReturnsSuccess()
        {
            var data = "{\"SessionLockedUntil\":\"2024-12-05T21:10:36.1193094+00:00\"}";

            IList<string> repeatedField = new List<string> { "test" };

            var bindingDataDictionary = new Dictionary<string, object>
            {
                { "SessionIdArray", repeatedField },
                { "SessionActions", JsonSerializer.Serialize(new
                    {
                        SessionLockedUntil = "2024-12-05T21:10:36.1193094+00:00"
                    })
                }
            };
            var context = new TestConverterContext(typeof(ServiceBusSessionMessageActions), data, new TestFunctionContext(new TestBindingContext(bindingDataDictionary)));
            var converter = new ServiceBusSessionMessageActionsConverter(new MockSettlementClient("test"));
            var result = await converter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Succeeded, result.Status);
            var output = result.Value as ServiceBusSessionMessageActions;
            Assert.NotNull(output);
        }

        [Fact]
        public async Task ConvertAsync_ReturnsFailure_NoSessionId()
        {
            var data = "{\"SessionLockedUntil\":\"2024-12-05T21:10:36.1193094+00:00\"}";

            var bindingDataDictionary = new Dictionary<string, object>
            {
                { "SessionActions", JsonSerializer.Serialize(new
                    {
                        SessionLockedUntil = "2024-12-05T21:10:36.1193094+00:00"
                    })
                }
            };
            var context = new TestConverterContext(typeof(ServiceBusSessionMessageActions), data, new TestFunctionContext(new TestBindingContext(bindingDataDictionary)));
            var converter = new ServiceBusSessionMessageActionsConverter(new MockSettlementClient("test"));
            var result = await converter.ConvertAsync(context);

            Assert.Equal(ConversionStatus.Failed, result.Status);
        }
    }
}
