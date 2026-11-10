// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Microsoft.Azure.Functions.Tests;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Azure.Functions.Worker.Core.Configuration;
using Microsoft.Azure.Functions.Worker.Grpc;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Azure.Functions.Worker.Handlers;
using Microsoft.Azure.Functions.Worker.Invocation;
using Microsoft.Azure.Functions.Worker.Tests.HostConfigurationDelivery;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests
{
    public class GrpcHostConfigurationTests
    {
        private static WorkerInitRequest Requested(string value = GrpcWorker.HostConfigurationCapabilityValue)
        {
            var request = new WorkerInitRequest();
            request.Capabilities[GrpcWorker.HostConfigurationCapabilityName] = value;
            return request;
        }

        [Theory]
        [InlineData(nameof(HostConfigurationStatus.Available))]
        [InlineData(nameof(HostConfigurationStatus.Missing))]
        [InlineData(nameof(HostConfigurationStatus.Error))]
        [InlineData(nameof(HostConfigurationStatus.TooLarge))]
        public void InitHandler_MapsCoreSnapshotToEnvelope_WithoutFailingInitialization(string outcome)
        {
            var status = Enum.Parse<HostConfigurationStatus>(outcome);
            var fs = new FakeFileSystem(() => status switch
            {
                HostConfigurationStatus.Available => new MemoryStream(Encoding.UTF8.GetBytes("{}")),
                HostConfigurationStatus.Missing => throw new HostConfigurationFileException(
                    HostConfigurationFailureReason.FileNotFound, new FileNotFoundException()),
                HostConfigurationStatus.Error => new FailingStream(),
                HostConfigurationStatus.TooLarge => new MemoryStream(new byte[HostConfigurationReader.MaxContentBytes + 1]),
                _ => throw new InvalidOperationException()
            });
            var provider = new HostConfigurationSnapshotProvider(fs);

            var response = GrpcWorker.WorkerInitRequestHandler(Requested(), new WorkerOptions(), provider);

            Assert.Equal(StatusResult.Types.Status.Success, response.Result.Status);
            Assert.Null(response.Result.Exception);
            Assert.Equal(Enum.Parse<HostConfiguration.Types.Status>(outcome), response.HostConfiguration.Status);
            if (status == HostConfigurationStatus.Available)
            {
                Assert.Equal("{}", response.HostConfiguration.Content.ToStringUtf8());
            }
            else
            {
                Assert.True(response.HostConfiguration.Content.IsEmpty);
            }

            Assert.DoesNotContain(GrpcWorker.HostConfigurationCapabilityName, response.Capabilities.Keys);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("0")]
        [InlineData("true")]
        [InlineData("2")]
        [InlineData(" 1")]
        [InlineData("1 ")]
        public void InitHandler_AbsentOrNonEnabledCapability_OmitsEnvelope_AndNeverTouchesFiles(string value)
        {
            var fs = new FakeFileSystem(() => throw new InvalidOperationException("The file must not be opened."));
            var provider = new HostConfigurationSnapshotProvider(fs);
            var request = value is null ? new WorkerInitRequest() : Requested(value);

            var response = GrpcWorker.WorkerInitRequestHandler(request, new WorkerOptions(), provider);

            Assert.Null(response.HostConfiguration);
            Assert.Equal(StatusResult.Types.Status.Success, response.Result.Status);
            Assert.Equal(0, fs.Opens);
        }

        [Fact]
        public void CapabilityName_IsCaseSensitive()
        {
            var fs = new FakeFileSystem(() => throw new InvalidOperationException("The file must not be opened."));
            var provider = new HostConfigurationSnapshotProvider(fs);
            var request = new WorkerInitRequest();
            request.Capabilities["hostconfiguration"] = "1";

            var response = GrpcWorker.WorkerInitRequestHandler(request, new WorkerOptions(), provider);

            Assert.Null(response.HostConfiguration);
            Assert.Equal(0, fs.Opens);
        }

        [Fact]
        public void InitHandler_HostSuppliedDirectory_DoesNotChangeApplicationRoot()
        {
            string opened = null;
            var fs = new FakeFileSystem(path =>
            {
                opened = path;
                return new MemoryStream(Encoding.UTF8.GetBytes("{}"));
            });
            var provider = new HostConfigurationSnapshotProvider(fs);
            var request = Requested();
            request.FunctionAppDirectory = Path.Combine(AppContext.BaseDirectory, "host-supplied");

            var response = GrpcWorker.WorkerInitRequestHandler(request, new WorkerOptions(), provider);

            Assert.Equal(HostConfiguration.Types.Status.Available, response.HostConfiguration.Status);
            Assert.Equal(Path.Combine(AppContext.BaseDirectory, "host.json"), opened);
        }

        [Fact]
        public void RequestedInit_CapturesOnce_AndUnrequestedInitOmitsEvenCachedSnapshot()
        {
            var fs = new FakeFileSystem(() => new MemoryStream(Encoding.UTF8.GetBytes("{}")));
            var provider = new HostConfigurationSnapshotProvider(fs);

            Assert.Null(GrpcWorker.WorkerInitRequestHandler(new WorkerInitRequest(), new WorkerOptions(), provider).HostConfiguration);
            Assert.Equal(0, fs.Opens);

            var requested = GrpcWorker.WorkerInitRequestHandler(Requested(), new WorkerOptions(), provider);
            var replay = GrpcWorker.WorkerInitRequestHandler(Requested(), new WorkerOptions(), provider);

            Assert.Equal(HostConfiguration.Types.Status.Available, requested.HostConfiguration.Status);
            Assert.NotSame(requested.HostConfiguration, replay.HostConfiguration);
            Assert.Equal(requested.HostConfiguration.Content, replay.HostConfiguration.Content);
            Assert.Null(GrpcWorker.WorkerInitRequestHandler(new WorkerInitRequest(), new WorkerOptions(), provider).HostConfiguration);
            Assert.Equal(1, fs.Opens);
        }

        [Fact]
        public void InitHandler_EnvelopeMutation_DoesNotChangeCachedSnapshot()
        {
            var fs = new FakeFileSystem(() => new MemoryStream(Encoding.UTF8.GetBytes("{}")));
            var provider = new HostConfigurationSnapshotProvider(fs);
            var first = GrpcWorker.WorkerInitRequestHandler(Requested(), new WorkerOptions(), provider);

            first.HostConfiguration.Content = ByteString.Empty;
            first.HostConfiguration.Status = HostConfiguration.Types.Status.Missing;
            var second = GrpcWorker.WorkerInitRequestHandler(Requested(), new WorkerOptions(), provider);

            Assert.Equal(HostConfiguration.Types.Status.Available, second.HostConfiguration.Status);
            Assert.Equal("{}", second.HostConfiguration.Content.ToStringUtf8());
            Assert.Equal(HostConfigurationStatus.Available, provider.GetSnapshot().Status);
            Assert.Equal("{}", Encoding.UTF8.GetString(provider.GetSnapshot().Content.Span));
            Assert.Equal(1, fs.Opens);
        }

        [Fact]
        public void InitHandler_DocumentAppearsOnlyInEnvelope()
        {
            const string marker = "SENSITIVE-MARKER-VALUE";
            var provider = new HostConfigurationSnapshotProvider(new FakeFileSystem(
                () => new MemoryStream(Encoding.UTF8.GetBytes("{\"x\":\"" + marker + "\"}"))));

            var response = GrpcWorker.WorkerInitRequestHandler(Requested(), new WorkerOptions(), provider);
            response.HostConfiguration = null;

            Assert.DoesNotContain(marker, response.ToString());
            Assert.DoesNotContain(AppContext.BaseDirectory, response.ToString());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task GrpcWorker_ProcessesInitThroughMessagePipeline(bool requested)
        {
            var fs = new FakeFileSystem(() => new MemoryStream(Encoding.UTF8.GetBytes("{}")));
            var provider = new HostConfigurationSnapshotProvider(fs);
            var sentSignal = new TaskCompletionSource<StreamingMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
            var client = new Mock<IWorkerClient>();
            client.Setup(c => c.SendMessageAsync(It.IsAny<StreamingMessage>()))
                .Returns((StreamingMessage message) => { sentSignal.TrySetResult(message); return default; });
            var factory = new Mock<IWorkerClientFactory>();
            factory.Setup(f => f.CreateClient(It.IsAny<IMessageProcessor>())).Returns(client.Object);
            var worker = new GrpcWorker(
                new Mock<IFunctionsApplication>().Object,
                factory.Object,
                new Mock<IMethodInfoLocator>().Object,
                new OptionsWrapper<WorkerOptions>(new WorkerOptions()),
                new Mock<IFunctionMetadataManager>().Object,
                new ApplicationLifetime(TestLogger<ApplicationLifetime>.Create()),
                new Mock<IInvocationHandler>().Object,
                provider);

            await worker.StartAsync(CancellationToken.None);
            await ((IMessageProcessor)worker).ProcessMessageAsync(new StreamingMessage
            {
                RequestId = "r1",
                WorkerInitRequest = requested ? Requested() : new WorkerInitRequest()
            });
            var sent = await sentSignal.Task.WaitAsync(TimeSpan.FromSeconds(10));

            Assert.Equal("r1", sent.RequestId);
            Assert.Equal(StatusResult.Types.Status.Success, sent.WorkerInitResponse.Result.Status);
            if (requested)
            {
                Assert.Equal(HostConfiguration.Types.Status.Available, sent.WorkerInitResponse.HostConfiguration.Status);
                Assert.Equal("{}", sent.WorkerInitResponse.HostConfiguration.Content.ToStringUtf8());
                Assert.Equal(1, fs.Opens);
            }
            else
            {
                Assert.Null(sent.WorkerInitResponse.HostConfiguration);
                Assert.Equal(0, fs.Opens);
            }
        }
    }
}
