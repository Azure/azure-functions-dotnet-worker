﻿using System;
using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Diagnostics;
using Microsoft.Azure.WebJobs.Script.Grpc.Messages;
using Microsoft.Extensions.Logging;
using Xunit;
using static Microsoft.Azure.WebJobs.Script.Grpc.Messages.RpcLog.Types;

namespace Microsoft.Azure.Functions.Worker.Tests.Diagnostics
{
    public class GrpcHostLoggerTests
    {
        private readonly GrpcFunctionsHostLoggerProvider _provider;
        private readonly Channel<StreamingMessage> _channel;

        public GrpcHostLoggerTests()
        {
            _channel = Channel.CreateUnbounded<StreamingMessage>();
            var outputChannel = new FunctionsHostOutputChannel(_channel);
            _provider = new GrpcFunctionsHostLoggerProvider(outputChannel);
            _provider.SetScopeProvider(new LoggerExternalScopeProvider());
        }

        [Fact]
        public async Task UserLog()
        {
            var logger = _provider.CreateLogger("TestLogger");

            logger.LogInformation("user");

            _channel.Writer.Complete();

            int count = 0;
            await foreach (var msg in _channel.Reader.ReadAllAsync())
            {
                count++;
                Assert.Equal("TestLogger", msg.RpcLog.Category);
                Assert.Equal(RpcLog.Types.RpcLogCategory.User, msg.RpcLog.LogCategory);
                Assert.Equal("user", msg.RpcLog.Message);
                Assert.Equal("0", msg.RpcLog.EventId);
            }

            Assert.Equal(1, count);
        }

        [Fact]
        public async Task SystemLog_WithException_AndScope()
        {
            var logger = _provider.CreateLogger("TestLogger");
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

            Assert.Collection(msgs,
                p =>
                {
                    Assert.Equal("TestLogger", p.RpcLog.Category);
                    Assert.Equal(RpcLogCategory.System, p.RpcLog.LogCategory);
                    Assert.Equal("system log with this", p.RpcLog.Message);
                    Assert.Equal("One", p.RpcLog.EventId);
                    Assert.Equal("MyInvocationId", p.RpcLog.InvocationId);
                    Assert.Equal(thrownException.ToString(), p.RpcLog.Exception.Message);
                    Assert.Equal("Microsoft.Azure.Functions.Worker.Tests", p.RpcLog.Exception.Source);
                    Assert.Contains(nameof(SystemLog_WithException_AndScope), p.RpcLog.Exception.StackTrace);
                },
                p =>
                {
                    Assert.Equal("TestLogger", p.RpcLog.Category);
                    Assert.Equal(RpcLogCategory.User, p.RpcLog.LogCategory);
                    Assert.Equal("user", p.RpcLog.Message);
                    Assert.Equal("0", p.RpcLog.EventId);
                    Assert.Equal("MyInvocationId", p.RpcLog.InvocationId);
                    Assert.Null(p.RpcLog.Exception);
                });
        }
    }
}
