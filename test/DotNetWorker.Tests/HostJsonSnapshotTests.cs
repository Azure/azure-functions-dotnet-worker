// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Microsoft.Azure.Functions.Worker.Grpc.HostJson;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Azure.Functions.Worker.Grpc;
using Microsoft.Azure.Functions.Worker.Handlers;
using Microsoft.Azure.Functions.Tests;
using Microsoft.Azure.Functions.Worker.Invocation;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests
{
    public class HostJsonSnapshotTests : IDisposable
    {
        private const int Limit = HostJsonSnapshotProvider.MaxHostJsonBytes;
        private readonly string _appDirectory = Path.Combine(Path.GetTempPath(), "hostjson-" + Guid.NewGuid().ToString("N"));

        public HostJsonSnapshotTests()
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

        private static WorkerInitRequest NegotiatedRequest(string value = "1")
        {
            var request = new WorkerInitRequest { FunctionAppDirectory = Path.Combine(Path.GetTempPath(), "host-supplied-must-be-ignored") };
            request.Capabilities["HostJson"] = value;
            return request;
        }

        private HostJsonSnapshotProvider PhysicalProvider() => new(() => _appDirectory, new PhysicalHostJsonFileSystem());

        private void WriteHostJson(byte[] bytes) => File.WriteAllBytes(Path.Combine(_appDirectory, "host.json"), bytes);

        private static HostJsonConfiguration Get(HostJsonSnapshotProvider provider) => provider.TryGetConfiguration(NegotiatedRequest());

        [Fact]
        public void ExactLimit_IsAvailable()
        {
            byte[] bytes = Enumerable.Repeat((byte)'a', Limit).ToArray();
            WriteHostJson(bytes);

            var result = Get(PhysicalProvider());

            Assert.Equal(HostJsonConfiguration.Types.Status.Available, result.Status);
            Assert.Equal(bytes, result.Content.ToByteArray());
        }

        [Fact]
        public void OneByteOverLimit_IsTooLarge_WithoutContent()
        {
            WriteHostJson(Enumerable.Repeat((byte)'a', Limit + 1).ToArray());

            var result = Get(PhysicalProvider());

            Assert.Equal(HostJsonConfiguration.Types.Status.TooLarge, result.Status);
            Assert.True(result.Content.IsEmpty);
        }

        [Fact]
        public void BomCountsTowardLimit()
        {
            byte[] bytes = new byte[Limit + 1];
            bytes[0] = 0xEF; bytes[1] = 0xBB; bytes[2] = 0xBF;
            WriteHostJson(bytes);

            Assert.Equal(HostJsonConfiguration.Types.Status.TooLarge, Get(PhysicalProvider()).Status);
        }

        [Fact]
        public void EmptyFile_IsAvailableEmpty_NotMissing()
        {
            WriteHostJson(Array.Empty<byte>());

            var result = Get(PhysicalProvider());

            Assert.Equal(HostJsonConfiguration.Types.Status.Available, result.Status);
            Assert.True(result.Content.IsEmpty);
        }

        [Fact]
        public void RawBytes_AreReturnedUnchanged_IncludingBomAndInvalidUtf8()
        {
            byte[] bytes = { 0xEF, 0xBB, 0xBF, (byte)'{', 0xFF, 0xFE, 0x00, 0xC0, (byte)'}' };
            WriteHostJson(bytes);

            var result = Get(PhysicalProvider());

            Assert.Equal(HostJsonConfiguration.Types.Status.Available, result.Status);
            Assert.Equal(bytes, result.Content.ToByteArray());
        }

        [Fact]
        public void MissingFile_WithValidAppRoot_IsMissing()
        {
            Assert.Equal(HostJsonConfiguration.Types.Status.Missing, Get(PhysicalProvider()).Status);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("relative/app")]
        public void InvalidAppRoot_IsReadError(string appRoot)
        {
            var provider = new HostJsonSnapshotProvider(() => appRoot, new PhysicalHostJsonFileSystem());

            Assert.Equal(HostJsonConfiguration.Types.Status.ReadError, Get(provider).Status);
        }

        [Fact]
        public void NonexistentAppRoot_IsReadError_NotMissing()
        {
            var provider = new HostJsonSnapshotProvider(() => Path.Combine(_appDirectory, "nope"), new PhysicalHostJsonFileSystem());

            Assert.Equal(HostJsonConfiguration.Types.Status.ReadError, Get(provider).Status);
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

            var request = NegotiatedRequest();
            request.FunctionAppDirectory = hostSupplied;
            var provider = new HostJsonSnapshotProvider(() => workerRoot, new PhysicalHostJsonFileSystem());

            Assert.Equal(HostJsonConfiguration.Types.Status.Missing, provider.TryGetConfiguration(request).Status);
        }

        [Fact]
        public void HostJsonIsADirectory_IsReadError()
        {
            Directory.CreateDirectory(Path.Combine(_appDirectory, "host.json"));

            Assert.Equal(HostJsonConfiguration.Types.Status.ReadError, Get(PhysicalProvider()).Status);
        }

        [Fact]
        public void ExclusivelyLockedFile_IsReadError()
        {
            string path = Path.Combine(_appDirectory, "host.json");
            File.WriteAllText(path, "{}");
            using var locked = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

            Assert.Equal(HostJsonConfiguration.Types.Status.ReadError, Get(PhysicalProvider()).Status);
        }

        [Fact]
        public void ConcurrentWriterSharingReadWrite_CanBeRead()
        {
            string path = Path.Combine(_appDirectory, "host.json");
            using var writer = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            writer.Write(new byte[] { (byte)'{', (byte)'}' }, 0, 2);
            writer.Flush();

            var result = Get(PhysicalProvider());

            Assert.Equal(HostJsonConfiguration.Types.Status.Available, result.Status);
            Assert.Equal(2, result.Content.Length);
        }

        [Fact]
        public void SeekableStream_WhoseLengthUnderReportsGrowth_IsStillBounded()
        {
            var fs = new FakeFileSystem(() => new UnderReportingStream(Limit + 100));

            var result = HostJsonSnapshotProvider.Read(FakeFileSystem.Root, fs);

            Assert.Equal(HostJsonConfiguration.Types.Status.TooLarge, result.Status);
        }

        [Fact]
        public void NonSeekableStream_ReturningOneByteAtATime_IsReadFully()
        {
            byte[] bytes = Enumerable.Range(0, 1000).Select(i => (byte)i).ToArray();
            var fs = new FakeFileSystem(() => new TrickleStream(bytes, int.MaxValue));

            var result = HostJsonSnapshotProvider.Read(FakeFileSystem.Root, fs);

            Assert.Equal(HostJsonConfiguration.Types.Status.Available, result.Status);
            Assert.Equal(bytes, result.Content.ToByteArray());
        }

        [Fact]
        public void NonSeekableStream_ExactlyAtLimit_IsAvailable_AndOneMoreIsTooLarge()
        {
            var exact = HostJsonSnapshotProvider.ReadBounded(new TrickleStream(new byte[Limit], int.MaxValue), Limit);
            var over = HostJsonSnapshotProvider.ReadBounded(new TrickleStream(new byte[Limit + 1], int.MaxValue), Limit);

            Assert.Equal(HostJsonConfiguration.Types.Status.Available, exact.Status);
            Assert.Equal(HostJsonConfiguration.Types.Status.TooLarge, over.Status);
        }

        [Fact]
        public void NeverReadsMuchBeyondLimit_ForUnboundedStream()
        {
            var stream = new TrickleStream(Array.Empty<byte>(), long.MaxValue, endless: true);

            var result = HostJsonSnapshotProvider.ReadBounded(stream, Limit);

            Assert.Equal(HostJsonConfiguration.Types.Status.TooLarge, result.Status);
            Assert.True(stream.TotalServed <= Limit + 1);
        }

        [Theory]
        [InlineData(typeof(IOException))]
        [InlineData(typeof(UnauthorizedAccessException))]
        [InlineData(typeof(NotSupportedException))]
        public void IoFailureOnOpen_IsContentFreeReadError(Type exceptionType)
        {
            var fs = new FakeFileSystem(() => throw (Exception)Activator.CreateInstance(exceptionType, "secret-path C:\\customer\\app"));

            var result = HostJsonSnapshotProvider.Read(FakeFileSystem.Root, fs);

            Assert.Equal(HostJsonConfiguration.Types.Status.ReadError, result.Status);
            Assert.True(result.Content.IsEmpty);
        }

        [Fact]
        public void IoFailureMidRead_IsReadError()
        {
            var fs = new FakeFileSystem(() => new FailingStream());

            Assert.Equal(HostJsonConfiguration.Types.Status.ReadError, HostJsonSnapshotProvider.Read(FakeFileSystem.Root, fs).Status);
        }

        [Fact]
        public void FileNotFoundOnOpen_IsMissing_AndDirectoryNotFoundIsReadError()
        {
            var missing = new FakeFileSystem(() => throw new FileNotFoundException());
            var racedAway = new FakeFileSystem(() => throw new DirectoryNotFoundException());

            Assert.Equal(HostJsonConfiguration.Types.Status.Missing, HostJsonSnapshotProvider.Read(FakeFileSystem.Root, missing).Status);
            Assert.Equal(HostJsonConfiguration.Types.Status.ReadError, HostJsonSnapshotProvider.Read(FakeFileSystem.Root, racedAway).Status);
        }

        [Fact]
        public void StreamIsDisposed_AfterRead()
        {
            var stream = new TrickleStream(new byte[] { 1 }, int.MaxValue);
            HostJsonSnapshotProvider.Read(FakeFileSystem.Root, new FakeFileSystem(() => stream));

            Assert.True(stream.Disposed);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("0")]
        [InlineData("2")]
        [InlineData("true")]
        [InlineData("1 ")]
        [InlineData("")]
        public void AbsentOrUnrecognizedCapability_ReturnsNoEnvelope_AndNeverTouchesFiles(string value)
        {
            var fs = new FakeFileSystem(() => new TrickleStream(new byte[] { 1 }, int.MaxValue));
            var provider = new HostJsonSnapshotProvider(() => FakeFileSystem.Root, fs);
            var request = new WorkerInitRequest();
            if (value is not null)
            {
                request.Capabilities["HostJson"] = value;
            }

            Assert.Null(provider.TryGetConfiguration(request));
            Assert.Equal(0, fs.DirectoryChecks + fs.Opens);
        }

        [Fact]
        public void CapabilityName_IsCaseSensitive()
        {
            var fs = new FakeFileSystem(() => new TrickleStream(new byte[] { 1 }, int.MaxValue));
            var provider = new HostJsonSnapshotProvider(() => FakeFileSystem.Root, fs);
            var request = new WorkerInitRequest();
            request.Capabilities["hostjson"] = "1";

            Assert.Null(provider.TryGetConfiguration(request));
            Assert.Equal(0, fs.Opens);
        }

        [Fact]
        public void RepeatedInit_ReplaysSameSnapshot_WithSingleRead()
        {
            var fs = new FakeFileSystem(() => new TrickleStream(new byte[] { (byte)'{', (byte)'}' }, int.MaxValue));
            var provider = new HostJsonSnapshotProvider(() => FakeFileSystem.Root, fs);

            var first = provider.TryGetConfiguration(NegotiatedRequest());
            var second = provider.TryGetConfiguration(NegotiatedRequest());

            Assert.Equal(1, fs.Opens);
            Assert.Equal(first.Content, second.Content);
            Assert.Equal(HostJsonConfiguration.Types.Status.Available, second.Status);
        }

        [Fact]
        public void FailedSnapshot_IsCached_UntilProcessRestart()
        {
            int attempt = 0;
            var fs = new FakeFileSystem(() => attempt++ == 0 ? throw new IOException() : new TrickleStream(new byte[] { 1 }, int.MaxValue));
            var provider = new HostJsonSnapshotProvider(() => FakeFileSystem.Root, fs);

            var first = provider.TryGetConfiguration(NegotiatedRequest());
            var second = provider.TryGetConfiguration(NegotiatedRequest());

            Assert.Equal(HostJsonConfiguration.Types.Status.ReadError, first.Status);
            Assert.Equal(HostJsonConfiguration.Types.Status.ReadError, second.Status);
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
            var provider = new HostJsonSnapshotProvider(() => FakeFileSystem.Root, fs);

            Task<HostJsonConfiguration>[] tasks = Enumerable.Range(0, 16)
                .Select(_ => Task.Run(() => provider.TryGetConfiguration(NegotiatedRequest())))
                .ToArray();
            await Task.Delay(100);
            gate.Set();
            var results = await Task.WhenAll(tasks);

            Assert.Equal(1, fs.Opens);
            Assert.All(results, r => Assert.Equal(new byte[] { 7, 8, 9 }, r.Content.ToByteArray()));
        }

        [Fact]
        public void InitHandler_NegotiatedAvailable_ReturnsEnvelope_AndSuccessResult()
        {
            WriteHostJson(new[] { (byte)'{', (byte)'}' });

            var response = GrpcWorker.WorkerInitRequestHandler(NegotiatedRequest(), new WorkerOptions(), PhysicalProvider());

            Assert.Equal(StatusResult.Types.Status.Success, response.Result.Status);
            Assert.Equal(HostJsonConfiguration.Types.Status.Available, response.HostJson.Status);
            Assert.Equal(new[] { (byte)'{', (byte)'}' }, response.HostJson.Content.ToByteArray());
        }

        [Fact]
        public void InitHandler_ReadFailure_DoesNotFailInitialization()
        {
            var response = GrpcWorker.WorkerInitRequestHandler(NegotiatedRequest(), new WorkerOptions(),
                new HostJsonSnapshotProvider(() => null, new PhysicalHostJsonFileSystem()));

            Assert.Equal(StatusResult.Types.Status.Success, response.Result.Status);
            Assert.Null(response.Result.Exception);
            Assert.Equal(HostJsonConfiguration.Types.Status.ReadError, response.HostJson.Status);
        }

        [Fact]
        public void InitHandler_WithoutCapability_HasNoEnvelope_AndUnchangedCapabilities()
        {
            WriteHostJson(new[] { (byte)'{', (byte)'}' });
            var withoutProvider = GrpcWorker.WorkerInitRequestHandler(new WorkerInitRequest(), new WorkerOptions());
            var withProvider = GrpcWorker.WorkerInitRequestHandler(new WorkerInitRequest(), new WorkerOptions(), PhysicalProvider());

            Assert.Null(withoutProvider.HostJson);
            Assert.Null(withProvider.HostJson);
            Assert.DoesNotContain("HostJson", withProvider.Capabilities.Keys);
            Assert.Equal(withoutProvider.Capabilities.OrderBy(k => k.Key), withProvider.Capabilities.OrderBy(k => k.Key));
        }

        [Fact]
        public void InitHandler_DocumentAppearsOnlyInEnvelope()
        {
            const string marker = "SENSITIVE-MARKER-VALUE";
            WriteHostJson(System.Text.Encoding.UTF8.GetBytes("{\"x\":\"" + marker + "\"}"));

            var response = GrpcWorker.WorkerInitRequestHandler(NegotiatedRequest(), new WorkerOptions(), PhysicalProvider());
            response.HostJson = null;

            Assert.DoesNotContain(marker, response.ToString());
            Assert.DoesNotContain(_appDirectory, response.ToString());
        }

        [Fact]
        public async Task GrpcWorker_ProcessesNegotiatedInitThroughMessagePipeline()
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
            await ((IMessageProcessor)worker).ProcessMessageAsync(new StreamingMessage { RequestId = "r1", WorkerInitRequest = NegotiatedRequest() });
            await sentSignal.Task.WaitAsync(TimeSpan.FromSeconds(10));

            Assert.Equal("r1", sent.RequestId);
            Assert.Equal(HostJsonConfiguration.Types.Status.Available, sent.WorkerInitResponse.HostJson.Status);
        }

        private sealed class FakeFileSystem : IHostJsonFileSystem
        {
            internal static readonly string Root = Path.Combine(Path.GetTempPath(), "fake-app-root");

            private readonly Func<Stream> _open;
            private int _opens;
            private int _directoryChecks;

            public FakeFileSystem(Func<Stream> open) => _open = open;

            public int Opens => _opens;

            public int DirectoryChecks => _directoryChecks;

            public bool DirectoryExists(string path)
            {
                Interlocked.Increment(ref _directoryChecks);
                return true;
            }

            public Stream OpenRead(string path)
            {
                Interlocked.Increment(ref _opens);
                return _open();
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
