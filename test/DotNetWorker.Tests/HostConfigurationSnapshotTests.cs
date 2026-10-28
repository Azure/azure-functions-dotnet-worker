// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Microsoft.Azure.Functions.Worker.Grpc.HostConfigurationDelivery;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Azure.Functions.Worker.Grpc;
using Microsoft.Azure.Functions.Worker.Handlers;
using Microsoft.Azure.Functions.Tests;
using Microsoft.Azure.Functions.Worker.Invocation;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests
{
    public class HostConfigurationSnapshotTests : IDisposable
    {
        private const int Limit = HostConfigurationSnapshotProvider.MaxHostConfigurationBytes;
        private readonly string _appDirectory = Path.Combine(Path.GetTempPath(), "hostjson-" + Guid.NewGuid().ToString("N"));

        public HostConfigurationSnapshotTests()
        {
            Directory.CreateDirectory(_appDirectory);
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(_appDirectory, recursive: true);
            }
            catch (IOException)
            {
            }
        }

        private HostConfigurationSnapshotProvider PhysicalProvider() => new(() => _appDirectory, new PhysicalHostConfigurationFileSystem());

        private void WriteHostJson(byte[] bytes) => File.WriteAllBytes(Path.Combine(_appDirectory, "host.json"), bytes);

        private static WorkerInitRequest Requested(string value = HostConfigurationSnapshotProvider.SupportedCapabilityValue)
        {
            var request = new WorkerInitRequest();
            request.Capabilities[HostConfigurationSnapshotProvider.CapabilityName] = value;
            return request;
        }

        private static HostConfiguration Get(HostConfigurationSnapshotProvider provider) => provider.TryGetConfiguration(Requested());

        [Fact]
        public void ExactLimit_IsAvailable()
        {
            byte[] bytes = Enumerable.Repeat((byte)'a', Limit).ToArray();
            WriteHostJson(bytes);

            var result = Get(PhysicalProvider());

            Assert.Equal(HostConfiguration.Types.Status.Available, result.Status);
            Assert.Equal(bytes, result.Content.ToByteArray());
        }

        [Fact]
        public void OneByteOverLimit_IsTooLarge_WithoutContent()
        {
            WriteHostJson(Enumerable.Repeat((byte)'a', Limit + 1).ToArray());

            var result = Get(PhysicalProvider());

            Assert.Equal(HostConfiguration.Types.Status.TooLarge, result.Status);
            Assert.True(result.Content.IsEmpty);
        }

        [Fact]
        public void Utf8Bom_IsStripped_AndLimitCountsConvertedBytes()
        {
            WriteHostJson(new byte[] { 0xEF, 0xBB, 0xBF }.Concat(Enumerable.Repeat((byte)'a', Limit)).ToArray());

            var result = Get(PhysicalProvider());

            Assert.Equal(HostConfiguration.Types.Status.Available, result.Status);
            Assert.Equal(Limit, result.Content.Length);
            Assert.NotEqual(0xEF, result.Content[0]);
        }

        [Fact]
        public void Utf8Bom_WithOneByteOverConvertedLimit_IsTooLarge()
        {
            WriteHostJson(new byte[] { 0xEF, 0xBB, 0xBF }.Concat(Enumerable.Repeat((byte)'a', Limit + 1)).ToArray());

            Assert.Equal(HostConfiguration.Types.Status.TooLarge, Get(PhysicalProvider()).Status);
        }

        public static IEnumerable<object[]> SourceEncodings() => new[]
        {
            new object[] { new UTF8Encoding(true) },
            new object[] { new UnicodeEncoding(false, true) },
            new object[] { new UnicodeEncoding(true, true) },
            new object[] { new UTF32Encoding(false, true) },
            new object[] { new UTF32Encoding(true, true) },
        };

        [Theory]
        [MemberData(nameof(SourceEncodings))]
        public void BomSources_AreConvertedToBomFreeUtf8(Encoding encoding)
        {
            const string text = "{\"a\":\"h\u00e9llo \u20ac \U0001F600\"}";
            WriteHostJson(encoding.GetPreamble().Concat(encoding.GetBytes(text)).ToArray());

            var result = Get(PhysicalProvider());

            Assert.Equal(HostConfiguration.Types.Status.Available, result.Status);
            Assert.Equal(new UTF8Encoding(false).GetBytes(text), result.Content.ToByteArray());
        }

        [Fact]
        public void Utf16Source_ExpandingToThreeByteUtf8_IsMeasuredAfterConversion()
        {
            // 21,845 euro signs are 43,690 UTF-16 bytes but 65,535 UTF-8 bytes; one more reaches 65,538.
            var encoding = new UnicodeEncoding(false, true);
            WriteHostJson(encoding.GetPreamble().Concat(encoding.GetBytes(new string('\u20ac', Limit / 3))).ToArray());
            var fits = Get(PhysicalProvider());

            Assert.Equal(HostConfiguration.Types.Status.Available, fits.Status);
            Assert.Equal(Limit / 3 * 3, fits.Content.Length);

            var over = HostConfigurationSnapshotProvider.ReadBounded(
                new MemoryStream(encoding.GetPreamble().Concat(encoding.GetBytes(new string('\u20ac', Limit / 3 + 1))).ToArray()));
            Assert.Equal(HostConfiguration.Types.Status.TooLarge, over.Status);
        }

        [Fact]
        public void Utf32Source_ContractingToOneByteUtf8_ExactLimitIsAvailable()
        {
            // 65,536 ASCII characters are 262,144 UTF-32 bytes (4x the cap) but exactly the converted cap.
            var encoding = new UTF32Encoding(false, true);
            WriteHostJson(encoding.GetPreamble().Concat(encoding.GetBytes(new string('a', Limit))).ToArray());

            var result = Get(PhysicalProvider());

            Assert.Equal(HostConfiguration.Types.Status.Available, result.Status);
            Assert.Equal(Limit, result.Content.Length);
        }

        [Fact]
        public void NoBomSource_IsTreatedAsUtf8()
        {
            byte[] bytes = new UTF8Encoding(false).GetBytes("{\"k\":\"\u00fc\"}");
            WriteHostJson(bytes);

            Assert.Equal(bytes, Get(PhysicalProvider()).Content.ToByteArray());
        }

        [Theory]
        [InlineData(new byte[] { (byte)'{', 0xFF, (byte)'}' })]
        [InlineData(new byte[] { 0xEF, 0xBB, 0xBF, (byte)'{', 0xC0, 0xAF, (byte)'}' })]
        [InlineData(new byte[] { (byte)'{', 0xE2, 0x82 })]
        [InlineData(new byte[] { 0xFF, 0xFE, (byte)'{', 0x00, 0x00, 0xD8 })]
        [InlineData(new byte[] { 0xFF, 0xFE, (byte)'{' })]
        [InlineData(new byte[] { 0xFE, 0xFF, 0xDC, 0x00, 0x00, (byte)'a' })]
        [InlineData(new byte[] { 0xFF, 0xFE, 0x00, 0x00, 0x00, 0x00, 0x11, 0x00 })]
        public void InvalidSourceEncoding_IsErrorWithoutContent(byte[] bytes)
        {
            WriteHostJson(bytes);

            var result = Get(PhysicalProvider());

            Assert.Equal(HostConfiguration.Types.Status.Error, result.Status);
            Assert.True(result.Content.IsEmpty);
        }

        [Fact]
        public void EmptyFile_IsAvailableEmpty_NotMissing()
        {
            WriteHostJson(Array.Empty<byte>());

            var result = Get(PhysicalProvider());

            Assert.Equal(HostConfiguration.Types.Status.Available, result.Status);
            Assert.True(result.Content.IsEmpty);
        }

        [Fact]
        public void ConvertedPayload_IsNotRequiredToBeJson_ButIsStrictUtf8()
        {
            byte[] bytes = Encoding.UTF8.GetBytes("not json");
            WriteHostJson(bytes);

            Assert.Equal(bytes, Get(PhysicalProvider()).Content.ToByteArray());
        }

        [Fact]
        public void MissingFile_WithValidAppRoot_IsMissing()
        {
            Assert.Equal(HostConfiguration.Types.Status.Missing, Get(PhysicalProvider()).Status);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void InvalidAppRoot_IsReadError(string appRoot)
        {
            var fs = new FakeFileSystem(() => new TrickleStream(new byte[] { 1 }, int.MaxValue));
            var provider = new HostConfigurationSnapshotProvider(() => appRoot, fs);

            Assert.Equal(HostConfiguration.Types.Status.Error, Get(provider).Status);
            Assert.Equal(0, fs.Opens);
        }

        [Fact]
        public void RelativeAppRoot_ReachesOpenRead_WithNativePathCombine()
        {
            string relative = Path.Combine("relative", "app");
            string opened = null;
            var fs = new FakeFileSystem(p => { opened = p; return new TrickleStream(new byte[] { 1 }, int.MaxValue); });

            var result = HostConfigurationSnapshotProvider.Read(relative, fs);

            Assert.Equal(HostConfiguration.Types.Status.Available, result.Status);
            Assert.Equal(Path.Combine(relative, "host.json"), opened);
        }

        [Fact]
        public void AccessorFailure_IsCachedContentFreeReadError_WithoutFileSystemAccess()
        {
            int calls = 0;
            var fs = new FakeFileSystem(() => new TrickleStream(new byte[] { 1 }, int.MaxValue));
            var provider = new HostConfigurationSnapshotProvider(() => { calls++; throw new InvalidOperationException("secret-path"); }, fs);

            var first = Get(provider);
            var second = Get(provider);

            Assert.Equal(HostConfiguration.Types.Status.Error, first.Status);
            Assert.True(first.Content.IsEmpty);
            Assert.Equal(HostConfiguration.Types.Status.Error, second.Status);
            Assert.Equal(1, calls);
            Assert.Equal(0, fs.Opens);
        }

        [Fact]
        public void AccessorIsNotInvoked_WithoutExactRequest()
        {
            int calls = 0;
            var fs = new FakeFileSystem(() => new TrickleStream(new byte[] { 1 }, int.MaxValue));
            var provider = new HostConfigurationSnapshotProvider(() => { calls++; return FakeFileSystem.Root; }, fs);

            Assert.Null(provider.TryGetConfiguration(new WorkerInitRequest()));
            Assert.Null(provider.TryGetConfiguration(Requested("0")));
            Assert.Equal(0, calls);
            Assert.Equal(0, fs.Opens);
        }

        [Fact]
        public void NonexistentAppRoot_IsReadError_NotMissing()
        {
            var provider = new HostConfigurationSnapshotProvider(() => Path.Combine(_appDirectory, "nope"), new PhysicalHostConfigurationFileSystem());

            Assert.Equal(HostConfiguration.Types.Status.Error, Get(provider).Status);
        }

        [Fact]
        public void AppRootIsReadFromEnvironmentOnly_NotHostSuppliedDirectory()
        {
            // The host-supplied function_app_directory contains a host.json, the worker-local root does not.
            string hostSupplied = Path.Combine(_appDirectory, "host-supplied");
            Directory.CreateDirectory(hostSupplied);
            File.WriteAllText(Path.Combine(hostSupplied, "host.json"), "{}");
            string workerRoot = Path.Combine(_appDirectory, "worker-root");
            Directory.CreateDirectory(workerRoot);

            var provider = new HostConfigurationSnapshotProvider(() => workerRoot, new PhysicalHostConfigurationFileSystem());

            Assert.Equal(HostConfiguration.Types.Status.Missing, provider.TryGetConfiguration(Requested()).Status);
        }

        [Fact]
        public void HostJsonIsADirectory_IsReadError()
        {
            Directory.CreateDirectory(Path.Combine(_appDirectory, "host.json"));

            Assert.Equal(HostConfiguration.Types.Status.Error, Get(PhysicalProvider()).Status);
        }

        [Fact]
        public void ExclusivelyLockedFile_IsReadError()
        {
            string path = Path.Combine(_appDirectory, "host.json");
            File.WriteAllText(path, "{}");
            using var locked = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

            Assert.Equal(HostConfiguration.Types.Status.Error, Get(PhysicalProvider()).Status);
        }

        [Fact]
        public void ConcurrentWriterSharingReadWrite_CanBeRead()
        {
            string path = Path.Combine(_appDirectory, "host.json");
            using var writer = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            writer.Write(new byte[] { (byte)'{', (byte)'}' }, 0, 2);
            writer.Flush();

            var result = Get(PhysicalProvider());

            Assert.Equal(HostConfiguration.Types.Status.Available, result.Status);
            Assert.Equal(2, result.Content.Length);
        }

        [Fact]
        public void SeekableStream_WhoseLengthUnderReportsGrowth_IsStillBounded()
        {
            var fs = new FakeFileSystem(() => new UnderReportingStream(Limit + 100));

            var result = HostConfigurationSnapshotProvider.Read(FakeFileSystem.Root, fs);

            Assert.Equal(HostConfiguration.Types.Status.TooLarge, result.Status);
        }

        [Fact]
        public void NonSeekableStream_ReturningOneByteAtATime_IsReadFully()
        {
            byte[] bytes = Enumerable.Range(0, 1000).Select(i => (byte)('a' + i % 26)).ToArray();
            var fs = new FakeFileSystem(() => new TrickleStream(bytes, int.MaxValue));

            var result = HostConfigurationSnapshotProvider.Read(FakeFileSystem.Root, fs);

            Assert.Equal(HostConfiguration.Types.Status.Available, result.Status);
            Assert.Equal(bytes, result.Content.ToByteArray());
        }

        [Fact]
        public void NonSeekableStream_ExactlyAtLimit_IsAvailable_AndOneMoreIsTooLarge()
        {
            var exact = HostConfigurationSnapshotProvider.ReadBounded(new TrickleStream(new byte[Limit], int.MaxValue));
            var over = HostConfigurationSnapshotProvider.ReadBounded(new TrickleStream(new byte[Limit + 1], int.MaxValue));

            Assert.Equal(HostConfiguration.Types.Status.Available, exact.Status);
            Assert.Equal(HostConfiguration.Types.Status.TooLarge, over.Status);
        }

        [Fact]
        public void NeverReadsMuchBeyondLimit_ForUnboundedStream()
        {
            var stream = new TrickleStream(Array.Empty<byte>(), long.MaxValue, endless: true);

            var result = HostConfigurationSnapshotProvider.ReadBounded(stream);

            Assert.Equal(HostConfiguration.Types.Status.TooLarge, result.Status);
            Assert.Equal(Limit + 1, stream.TotalServed);
        }

        [Theory]
        [InlineData(typeof(IOException))]
        [InlineData(typeof(UnauthorizedAccessException))]
        [InlineData(typeof(NotSupportedException))]
        public void IoFailureOnOpen_IsContentFreeReadError(Type exceptionType)
        {
            var fs = new FakeFileSystem(() => throw (Exception)Activator.CreateInstance(exceptionType, "secret-path C:\\customer\\app"));

            var result = HostConfigurationSnapshotProvider.Read(FakeFileSystem.Root, fs);

            Assert.Equal(HostConfiguration.Types.Status.Error, result.Status);
            Assert.True(result.Content.IsEmpty);
        }

        [Fact]
        public void IoFailureMidRead_IsReadError()
        {
            var fs = new FakeFileSystem(() => new FailingStream());

            Assert.Equal(HostConfiguration.Types.Status.Error, HostConfigurationSnapshotProvider.Read(FakeFileSystem.Root, fs).Status);
        }

        [Fact]
        public void FileNotFoundOnOpen_IsMissing_AndDirectoryNotFoundIsReadError()
        {
            var missing = new FakeFileSystem(() => throw new FileNotFoundException());
            var racedAway = new FakeFileSystem(() => throw new DirectoryNotFoundException());

            Assert.Equal(HostConfiguration.Types.Status.Missing, HostConfigurationSnapshotProvider.Read(FakeFileSystem.Root, missing).Status);
            Assert.Equal(HostConfiguration.Types.Status.Error, HostConfigurationSnapshotProvider.Read(FakeFileSystem.Root, racedAway).Status);
        }

        [Fact]
        public void StreamIsDisposed_AfterRead()
        {
            var stream = new TrickleStream(new byte[] { 1 }, int.MaxValue);
            HostConfigurationSnapshotProvider.Read(FakeFileSystem.Root, new FakeFileSystem(() => stream));

            Assert.True(stream.Disposed);
        }

        [Fact]
        public void RepeatedInit_ReplaysSameSnapshot_WithSingleRead()
        {
            var fs = new FakeFileSystem(() => new TrickleStream(new byte[] { (byte)'{', (byte)'}' }, int.MaxValue));
            var provider = new HostConfigurationSnapshotProvider(() => FakeFileSystem.Root, fs);

            var first = provider.TryGetConfiguration(Requested());
            var second = provider.TryGetConfiguration(Requested());

            Assert.Equal(1, fs.Opens);
            Assert.Equal(first.Content, second.Content);
            Assert.Equal(HostConfiguration.Types.Status.Available, second.Status);
        }

        [Fact]
        public void FailedSnapshot_IsCached_UntilProcessRestart()
        {
            int attempt = 0;
            var fs = new FakeFileSystem(() => attempt++ == 0 ? throw new IOException() : new TrickleStream(new byte[] { 1 }, int.MaxValue));
            var provider = new HostConfigurationSnapshotProvider(() => FakeFileSystem.Root, fs);

            var first = provider.TryGetConfiguration(Requested());
            var second = provider.TryGetConfiguration(Requested());

            Assert.Equal(HostConfiguration.Types.Status.Error, first.Status);
            Assert.Equal(HostConfiguration.Types.Status.Error, second.Status);
            Assert.Equal(1, fs.Opens);
        }

        [Fact]
        public void FileChangesAfterFirstInit_AreNotObserved()
        {
            WriteHostJson(new[] { (byte)'1' });
            var provider = PhysicalProvider();
            Get(provider);

            WriteHostJson(new[] { (byte)'2', (byte)'2' });

            Assert.Equal(new[] { (byte)'1' }, Get(provider).Content.ToByteArray());
        }

        [Fact]
        public async Task ConcurrentInit_ReadsOnce_AndAllCallersSeeSameContent()
        {
            var gate = new ManualResetEventSlim();
            var fs = new FakeFileSystem(() =>
            {
                gate.Wait();
                return new TrickleStream(new byte[] { 7, 8, 9 }, int.MaxValue);
            });
            var provider = new HostConfigurationSnapshotProvider(() => FakeFileSystem.Root, fs);

            Task<HostConfiguration>[] tasks = Enumerable.Range(0, 16)
                .Select(_ => Task.Run(() => provider.TryGetConfiguration(Requested())))
                .ToArray();
            await Task.Delay(100);
            gate.Set();
            var results = await Task.WhenAll(tasks);

            Assert.Equal(1, fs.Opens);
            Assert.All(results, r => Assert.Equal(new byte[] { 7, 8, 9 }, r.Content.ToByteArray()));
        }

        [Fact]
        public void InitHandler_Available_ReturnsEnvelope_AndSuccessResult()
        {
            WriteHostJson(new[] { (byte)'{', (byte)'}' });

            var response = GrpcWorker.WorkerInitRequestHandler(Requested(), new WorkerOptions(), PhysicalProvider());

            Assert.Equal(StatusResult.Types.Status.Success, response.Result.Status);
            Assert.Equal(HostConfiguration.Types.Status.Available, response.HostConfiguration.Status);
            Assert.Equal(new[] { (byte)'{', (byte)'}' }, response.HostConfiguration.Content.ToByteArray());
        }

        [Fact]
        public void InitHandler_ReadFailure_DoesNotFailInitialization()
        {
            var response = GrpcWorker.WorkerInitRequestHandler(Requested(), new WorkerOptions(),
                new HostConfigurationSnapshotProvider(() => null, new PhysicalHostConfigurationFileSystem()));

            Assert.Equal(StatusResult.Types.Status.Success, response.Result.Status);
            Assert.Null(response.Result.Exception);
            Assert.Equal(HostConfiguration.Types.Status.Error, response.HostConfiguration.Status);
        }

        [Fact]
        public void InitHandler_Requested_DoesNotAdvertiseCapabilityInResponse()
        {
            WriteHostJson(new[] { (byte)'{', (byte)'}' });

            var response = GrpcWorker.WorkerInitRequestHandler(Requested(), new WorkerOptions(), PhysicalProvider());

            Assert.Equal(HostConfiguration.Types.Status.Available, response.HostConfiguration.Status);
            Assert.DoesNotContain(HostConfigurationSnapshotProvider.CapabilityName, response.Capabilities.Keys);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("0")]
        [InlineData("true")]
        [InlineData("2")]
        [InlineData(" 1")]
        public void InitHandler_AbsentOrNonEnabledCapability_OmitsEnvelope_AndNeverTouchesFiles(string value)
        {
            var fs = new FakeFileSystem(() => new TrickleStream(new byte[] { 1 }, int.MaxValue));
            var provider = new HostConfigurationSnapshotProvider(() => FakeFileSystem.Root, fs);
            var request = new WorkerInitRequest();
            if (value is not null)
            {
                request.Capabilities[HostConfigurationSnapshotProvider.CapabilityName] = value;
            }

            var response = GrpcWorker.WorkerInitRequestHandler(request, new WorkerOptions(), provider);

            Assert.Null(response.HostConfiguration);
            Assert.Equal(StatusResult.Types.Status.Success, response.Result.Status);
            Assert.Equal(0, fs.Opens);
        }

        [Fact]
        public void CapabilityName_IsCaseSensitive()
        {
            var fs = new FakeFileSystem(() => new TrickleStream(new byte[] { 1 }, int.MaxValue));
            var provider = new HostConfigurationSnapshotProvider(() => FakeFileSystem.Root, fs);
            var request = new WorkerInitRequest();
            request.Capabilities["hostconfiguration"] = "1";

            Assert.Null(provider.TryGetConfiguration(request));
            Assert.Equal(0, fs.Opens);
        }

        [Fact]
        public void EarlierUnrequestedInit_DoesNotCaptureSnapshot_BeforeLaterRequestedInit()
        {
            var fs = new FakeFileSystem(() => new TrickleStream(new byte[] { 1 }, int.MaxValue));
            var provider = new HostConfigurationSnapshotProvider(() => FakeFileSystem.Root, fs);

            Assert.Null(GrpcWorker.WorkerInitRequestHandler(new WorkerInitRequest(), new WorkerOptions(), provider).HostConfiguration);
            Assert.Equal(0, fs.Opens);

            var requested = GrpcWorker.WorkerInitRequestHandler(Requested(), new WorkerOptions(), provider);
            var replay = GrpcWorker.WorkerInitRequestHandler(Requested(), new WorkerOptions(), provider);

            Assert.Equal(HostConfiguration.Types.Status.Available, requested.HostConfiguration.Status);
            Assert.NotSame(requested.HostConfiguration, replay.HostConfiguration);
            Assert.Equal(1, fs.Opens);
        }

        [Fact]
        public async Task GrpcWorker_WithoutCapability_SendsInitWithoutEnvelope()
        {
            WriteHostJson(new[] { (byte)'{', (byte)'}' });
            StreamingMessage sent = null;
            var sentSignal = new TaskCompletionSource<bool>();
            var client = new Moq.Mock<IWorkerClient>();
            client.Setup(c => c.SendMessageAsync(Moq.It.IsAny<StreamingMessage>()))
                .Returns((StreamingMessage m) => { sent = m; sentSignal.TrySetResult(true); return default; });
            var factory = new Moq.Mock<IWorkerClientFactory>();
            factory.Setup(f => f.CreateClient(Moq.It.IsAny<IMessageProcessor>())).Returns(client.Object);

            var worker = new GrpcWorker(
                new Moq.Mock<IFunctionsApplication>().Object,
                factory.Object,
                new Moq.Mock<IMethodInfoLocator>().Object,
                new Microsoft.Extensions.Options.OptionsWrapper<WorkerOptions>(new WorkerOptions()),
                new Moq.Mock<Microsoft.Azure.Functions.Worker.Core.FunctionMetadata.IFunctionMetadataManager>().Object,
                new Microsoft.Extensions.Hosting.Internal.ApplicationLifetime(TestLogger<Microsoft.Extensions.Hosting.Internal.ApplicationLifetime>.Create()),
                new Moq.Mock<IInvocationHandler>().Object,
                PhysicalProvider());

            await worker.StartAsync(CancellationToken.None);
            await ((IMessageProcessor)worker).ProcessMessageAsync(new StreamingMessage { RequestId = "r2", WorkerInitRequest = new WorkerInitRequest() });
            await sentSignal.Task.WaitAsync(TimeSpan.FromSeconds(10));

            Assert.Equal(StatusResult.Types.Status.Success, sent.WorkerInitResponse.Result.Status);
            Assert.Null(sent.WorkerInitResponse.HostConfiguration);
        }
        [Fact]
        public void InitHandler_EachResponseGetsItsOwnEnvelope_WithSameContent()
        {
            WriteHostJson(new[] { (byte)'{', (byte)'}' });
            var provider = PhysicalProvider();

            var first = GrpcWorker.WorkerInitRequestHandler(Requested(), new WorkerOptions(), provider);
            var second = GrpcWorker.WorkerInitRequestHandler(Requested(), new WorkerOptions(), provider);

            Assert.NotSame(first.HostConfiguration, second.HostConfiguration);
            Assert.Equal(first.HostConfiguration.Content, second.HostConfiguration.Content);
        }

        [Fact]
        public void InitHandler_DocumentAppearsOnlyInEnvelope()
        {
            const string marker = "SENSITIVE-MARKER-VALUE";
            WriteHostJson(System.Text.Encoding.UTF8.GetBytes("{\"x\":\"" + marker + "\"}"));

            var response = GrpcWorker.WorkerInitRequestHandler(Requested(), new WorkerOptions(), PhysicalProvider());
            response.HostConfiguration = null;

            Assert.DoesNotContain(marker, response.ToString());
            Assert.DoesNotContain(_appDirectory, response.ToString());
        }

        [Fact]
        public async Task GrpcWorker_ProcessesInitThroughMessagePipeline()
        {
            WriteHostJson(new[] { (byte)'{', (byte)'}' });
            StreamingMessage sent = null;
            var sentSignal = new TaskCompletionSource<bool>();
            var client = new Moq.Mock<IWorkerClient>();
            client.Setup(c => c.SendMessageAsync(Moq.It.IsAny<StreamingMessage>()))
                .Returns((StreamingMessage m) => { sent = m; sentSignal.TrySetResult(true); return default; });
            var factory = new Moq.Mock<IWorkerClientFactory>();
            factory.Setup(f => f.CreateClient(Moq.It.IsAny<IMessageProcessor>())).Returns(client.Object);

            var worker = new GrpcWorker(
                new Moq.Mock<IFunctionsApplication>().Object,
                factory.Object,
                new Moq.Mock<IMethodInfoLocator>().Object,
                new Microsoft.Extensions.Options.OptionsWrapper<WorkerOptions>(new WorkerOptions()),
                new Moq.Mock<Microsoft.Azure.Functions.Worker.Core.FunctionMetadata.IFunctionMetadataManager>().Object,
                new Microsoft.Extensions.Hosting.Internal.ApplicationLifetime(TestLogger<Microsoft.Extensions.Hosting.Internal.ApplicationLifetime>.Create()),
                new Moq.Mock<IInvocationHandler>().Object,
                PhysicalProvider());

            await worker.StartAsync(CancellationToken.None);
            await ((IMessageProcessor)worker).ProcessMessageAsync(new StreamingMessage { RequestId = "r1", WorkerInitRequest = Requested() });
            await sentSignal.Task.WaitAsync(TimeSpan.FromSeconds(10));

            Assert.Equal("r1", sent.RequestId);
            Assert.Equal(HostConfiguration.Types.Status.Available, sent.WorkerInitResponse.HostConfiguration.Status);
        }

        private sealed class FakeFileSystem : IHostConfigurationFileSystem
        {
            internal static readonly string Root = Path.Combine(Path.GetTempPath(), "fake-app-root");

            private readonly Func<string, Stream> _open;
            private int _opens;

            public FakeFileSystem(Func<Stream> open) => _open = _ => open();

            public FakeFileSystem(Func<string, Stream> open) => _open = open;

            public int Opens => _opens;

            public Stream OpenRead(string path)
            {
                Interlocked.Increment(ref _opens);
                return _open(path);
            }
        }

        private class TrickleStream : Stream
        {
            private readonly byte[] _data;
            private readonly long _maxBytes;
            private readonly bool _endless;
            private long _position;

            public TrickleStream(byte[] data, long maxBytes, bool endless = false)
            {
                _data = data;
                _maxBytes = maxBytes;
                _endless = endless;
            }

            public long TotalServed => _position;

            public bool Disposed { get; private set; }

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();
            public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
            public override void Flush() { }
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (count == 0 || (!_endless && _position >= _data.Length) || _position >= _maxBytes)
                {
                    return 0;
                }

                // Return a single byte per call to exercise the read loop.
                buffer[offset] = _endless ? (byte)'x' : _data[_position];
                _position++;
                return 1;
            }

            protected override void Dispose(bool disposing)
            {
                Disposed = true;
                base.Dispose(disposing);
            }
        }

        // Seekable stream that reports a small Length but keeps producing data, like a file that grows while read.
        private sealed class UnderReportingStream : Stream
        {
            private readonly int _total;
            private int _served;

            public UnderReportingStream(int total) => _total = total;

            public override bool CanRead => true;
            public override bool CanSeek => true;
            public override bool CanWrite => false;
            public override long Length => 10;
            public override long Position { get => _served; set => throw new NotSupportedException(); }
            public override void Flush() { }
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

            public override int Read(byte[] buffer, int offset, int count)
            {
                int n = Math.Min(count, _total - _served);
                for (int i = 0; i < n; i++)
                {
                    buffer[offset + i] = (byte)'a';
                }

                _served += n;
                return n;
            }
        }

        private sealed class FailingStream : TrickleStream
        {
            public FailingStream() : base(new byte[10], int.MaxValue)
            {
            }

            public override int Read(byte[] buffer, int offset, int count) => throw new IOException("disk error at C:\\customer\\app");
        }
    }
}
