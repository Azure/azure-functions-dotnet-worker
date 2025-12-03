// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker.Diagnostics;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Azure.Functions.Worker.Logging;
using Microsoft.Azure.Functions.Worker.Logging.ApplicationInsights;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;
using static Microsoft.Azure.Functions.Worker.Grpc.Messages.RpcLog.Types;

namespace Microsoft.Azure.Functions.Worker.Tests.Diagnostics
{
    public class GrpcHostLogWriterTests
    {
        private readonly Channel<StreamingMessage> _channel = Channel.CreateUnbounded<StreamingMessage>();
        private readonly IExternalScopeProvider _scopeProvider = new LoggerExternalScopeProvider();
        private readonly GrpcFunctionsHostLogWriter _logWriter;
        private readonly Func<string, Exception, string> _formatter = (s, e) => s;

        public GrpcHostLogWriterTests()
        {
            var outputChannel = new GrpcHostChannel(_channel);
            var workerOptions = Options.Create(new WorkerOptions { Serializer = new JsonObjectSerializer() });
            _logWriter = new GrpcFunctionsHostLogWriter(outputChannel, workerOptions);
        }

        [Fact]
        public async Task UserLog()
        {
            _logWriter.WriteUserLog(_scopeProvider, "TestLogger", LogLevel.Information, default(EventId), "user", null, _formatter);

            _channel.Writer.Complete();

            int count = 0;
            await foreach (var msg in _channel.Reader.ReadAllAsync())
            {
                count++;
                Assert.Equal(StreamingMessage.ContentOneofCase.RpcLog, msg.ContentCase);
                Assert.NotNull(msg.RpcLog);
                Assert.Equal("TestLogger", msg.RpcLog.Category);
                Assert.Equal(RpcLog.Types.RpcLogCategory.User, msg.RpcLog.LogCategory);
                Assert.Equal("user", msg.RpcLog.Message);
                Assert.Equal("0", msg.RpcLog.EventId);
            }

            Assert.Equal(1, count);
        }

        [Fact]
        public async Task CustomMetric()
        {
            _logWriter.WriteUserMetric(_scopeProvider, new Dictionary<string, object>
            {
                {"Name", "testMetric" },
                {"Value", 1d },
                {"foo", "bar" }
            });

            _channel.Writer.Complete();

            int count = 0;
            await foreach (var msg in _channel.Reader.ReadAllAsync())
            {
                count++;

                Assert.Equal(StreamingMessage.ContentOneofCase.RpcLog, msg.ContentCase);
                Assert.NotNull(msg.RpcLog);
                Assert.Equal(RpcLogCategory.CustomMetric, msg.RpcLog.LogCategory);

                var propertyBag = msg.RpcLog.PropertiesMap?.ToDictionary(i => i.Key, i => i.Value);
                Assert.NotNull(propertyBag);
                var metricName = propertyBag[LogConstants.NameKey];
                Assert.Equal(TypedData.DataOneofCase.String, metricName.DataCase);
                Assert.Equal("testMetric", metricName.String);
                var metricValue = propertyBag[LogConstants.MetricValueKey];
                Assert.Equal(TypedData.DataOneofCase.Double, metricValue.DataCase);
                Assert.Equal(1d, metricValue.Double);

                Assert.Equal("bar", propertyBag["foo"].String);
            }

            Assert.Equal(1, count);
        }

        [Fact]
        public async Task SystemLog_WithException_AndScope()
        {
            Exception thrownException = null;

            var scopeValues = new Dictionary<string, object>
            {
                ["AzureFunctions_InvocationId"] = "MyInvocationId",
                ["AzureFunctions_FunctionName"] = "MyFunction"
            };

            using (_scopeProvider.Push(scopeValues))
            {
                try
                {
                    throw new InvalidOperationException("boom");
                }
                catch (Exception ex)
                {
                    _logWriter.WriteSystemLog(_scopeProvider, "TestLogger", LogLevel.Trace, new EventId(1, "One"), "system log", ex, _formatter);
                    thrownException = ex;
                }
            }

            _channel.Writer.Complete();

            IList<StreamingMessage> msgs = new List<StreamingMessage>();

            await foreach (var msg in _channel.Reader.ReadAllAsync())
            {
                msgs.Add(msg);
            }

            List<Action<StreamingMessage>> expected = new();
            expected.Add(p =>
            {
                Assert.Equal("TestLogger", p.RpcLog.Category);
                Assert.Equal(RpcLogCategory.System, p.RpcLog.LogCategory);
                Assert.Equal("system log", p.RpcLog.Message);
                Assert.Equal("One", p.RpcLog.EventId);
                Assert.Equal("MyInvocationId", p.RpcLog.InvocationId);
                Assert.Equal(thrownException.ToString(), p.RpcLog.Exception.Message);
                Assert.Equal("Microsoft.Azure.Functions.Worker.Tests", p.RpcLog.Exception.Source);
                Assert.Contains(nameof(SystemLog_WithException_AndScope), p.RpcLog.Exception.StackTrace);
            });

            Assert.Collection(msgs, expected.ToArray());
        }
    }
}
