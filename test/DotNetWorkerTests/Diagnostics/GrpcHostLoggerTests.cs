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
    public class GrpcHostLoggerTests
    {
        private readonly Channel<StreamingMessage> _channel;

        public GrpcHostLoggerTests()
        {
            _channel = Channel.CreateUnbounded<StreamingMessage>();
        }

        private GrpcFunctionsHostLoggerProvider GetProvider(bool disableHostLogger)
        {
            var outputChannel = new GrpcHostChannel(_channel);
            var workerOptions = Options.Create(new WorkerOptions { Serializer = new JsonObjectSerializer(), DisableHostLogger = disableHostLogger });
            var provider = new GrpcFunctionsHostLoggerProvider(outputChannel, workerOptions);
            provider.SetScopeProvider(new LoggerExternalScopeProvider());

            return provider;
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task UserLog(bool disableHostLogger)
        {
            var logger = GetProvider(disableHostLogger).CreateLogger("TestLogger");

            logger.LogInformation("user");

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

            int expected = disableHostLogger ? 0 : 1;
            Assert.Equal(expected, count);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task CustomMetric(bool disableHostLogger)
        {
            // CustomMetrics are written to the host whether host logging is disabled or not
            var logger = GetProvider(disableHostLogger).CreateLogger("TestLogger");

            logger.LogMetric("testMetric", 1d, new Dictionary<string, object>
            {
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

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task SystemLog_WithException_AndScope(bool disableHostLogger)
        {
            var logger = GetProvider(disableHostLogger).CreateLogger("TestLogger");
            Exception thrownException = null;

            using (logger.BeginScope(new FunctionInvocationScope("MyFunction", "MyInvocationId")))
            {
                try
                {
                    throw new InvalidOperationException("boom");
                }
                catch (Exception ex)
                {
                    // The only way to log a system log.
                    var log = WorkerMessage.Define<string>(LogLevel.Trace, new EventId(1, "One"), "system log with {param}");
                    log(logger, "this", ex);
                    thrownException = ex;
                }

                // make sure this is now user
                logger.LogInformation("user");
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
                Assert.Equal("system log with this", p.RpcLog.Message);
                Assert.Equal("One", p.RpcLog.EventId);
                Assert.Equal("MyInvocationId", p.RpcLog.InvocationId);
                Assert.Equal(thrownException.ToString(), p.RpcLog.Exception.Message);
                Assert.Equal("Microsoft.Azure.Functions.Worker.Tests", p.RpcLog.Exception.Source);
                Assert.Contains(nameof(SystemLog_WithException_AndScope), p.RpcLog.Exception.StackTrace);
            });

            if (!disableHostLogger)
            {
                // User logs are ignored when host logger is disabled
                expected.Add(p =>
                {
                    Assert.Equal("TestLogger", p.RpcLog.Category);
                    Assert.Equal(RpcLogCategory.User, p.RpcLog.LogCategory);
                    Assert.Equal("user", p.RpcLog.Message);
                    Assert.Equal("0", p.RpcLog.EventId);
                    Assert.Equal("MyInvocationId", p.RpcLog.InvocationId);
                    Assert.Null(p.RpcLog.Exception);
                });
            }

            Assert.Collection(msgs, expected.ToArray());
        }
    }
}
