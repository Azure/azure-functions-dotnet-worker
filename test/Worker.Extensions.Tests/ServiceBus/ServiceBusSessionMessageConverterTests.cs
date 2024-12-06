// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Google.Protobuf;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.ServiceBus;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Azure.Functions.Worker.Tests;
using Microsoft.Azure.Functions.Worker.Tests.Converters;
using Moq;
using Xunit;
using Google.Protobuf.Collections;
using Microsoft.Azure.ServiceBus.Grpc;
using Microsoft.Azure.Functions.Worker.Extensions.Tests.ServiceBus;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;

namespace Microsoft.Azure.Functions.Worker.Extensions.Tests
{
    public class ServiceBusSessionMessageConverterTests
    {
        internal sealed class TestBindingContext : BindingContext
        {
            public TestBindingContext(IReadOnlyDictionary<string, object?> input)
            {
                BindingData = input;
            }

            public override IReadOnlyDictionary<string, object?> BindingData { get; }
        }

        [Fact]
        public async Task ConvertAsync_ReturnsSuccess()
        {
            var data = "{\"SessionLockedUntil\":\"2024-12-05T21:10:36.1193094+00:00\"}";
            
            var jsonObject = new
            {
                SessionLockedUntil = "2024-12-05T21:10:36.1193094+00:00"
            };
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

            var jsonObject = new
            {
                SessionLockedUntil = "2024-12-05T21:10:36.1193094+00:00"
            };
            RepeatedField<string> repeatedField = new RepeatedField<string>();
            repeatedField.Add("test");

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

            var jsonObject = new
            {
                SessionLockedUntil = "2024-12-05T21:10:36.1193094+00:00"
            };
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


        [Fact]
        public async Task ConvertAsync_Batch_ForReceivedMessage_ReturnsFailed_MultipleMessages()
        {
            var data = "{\"SessionLockedUntil\":\"2024-12-05T21:10:36.1193094+00:00\"}";

            var jsonObject = new
            {
                SessionLockedUntil = "2024-12-05T21:10:36.1193094+00:00"
            };
            RepeatedField<string> repeatedField = new RepeatedField<string>();
            repeatedField.Add("test");
            repeatedField.Add("hi");

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

            Assert.Equal(ConversionStatus.Failed, result.Status);
        }
    }
}
