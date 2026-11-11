// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Tests;
using Microsoft.Azure.Functions.Worker.Core.Configuration;
using Microsoft.Azure.Functions.Worker.Tests.HostConfigurationDelivery;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests
{
    public class HostConfigurationSnapshotTests : IDisposable
    {
        private const int Limit = HostConfigurationReader.MaxContentBytes;
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

        private HostConfigurationSnapshotProvider PhysicalProvider(ILogger logger = null)
            => new(new FakeFileSystem(() => new PhysicalHostConfigurationFileSystem().OpenRead(Path.Combine(_appDirectory, "host.json"))), logger);

        private void WriteHostJson(byte[] bytes) => File.WriteAllBytes(Path.Combine(_appDirectory, "host.json"), bytes);

        private static HostConfigurationSnapshot Get(HostConfigurationSnapshotProvider provider) => provider.GetSnapshot();

        private static byte[] JsonOfLength(int total)
            => Encoding.UTF8.GetBytes("{\"k\":\"" + new string('a', total - 8) + "\"}");

        [Fact]
        public void EmptyObject_IsAvailable()
        {
            WriteHostJson(Encoding.UTF8.GetBytes("{}"));

            var result = Get(PhysicalProvider());

            Assert.Equal(HostConfigurationStatus.Available, result.Status);
            Assert.Equal("{}", Encoding.UTF8.GetString(result.Content.Span));
        }

        [Fact]
        public void ExactLimit_IsAvailable()
        {
            byte[] bytes = JsonOfLength(Limit);
            WriteHostJson(bytes);

            var result = Get(PhysicalProvider());

            Assert.Equal(HostConfigurationStatus.Available, result.Status);
            Assert.Equal(bytes, result.Content.ToArray());
        }

        [Fact]
        public void OneByteOverLimit_IsTooLarge_WithoutContent()
        {
            WriteHostJson(JsonOfLength(Limit + 1));

            var result = Get(PhysicalProvider());

            Assert.Equal(HostConfigurationStatus.TooLarge, result.Status);
            Assert.True(result.Content.IsEmpty);
        }

        [Fact]
        public void Utf8Bom_IsStripped_AndLimitCountsConvertedBytes()
        {
            WriteHostJson(new byte[] { 0xEF, 0xBB, 0xBF }.Concat(JsonOfLength(Limit)).ToArray());

            var result = Get(PhysicalProvider());

            Assert.Equal(HostConfigurationStatus.Available, result.Status);
            Assert.Equal(Limit, result.Content.Length);
            Assert.NotEqual(0xEF, result.Content.Span[0]);
        }

        [Fact]
        public void Utf8Bom_WithOneByteOverConvertedLimit_IsTooLarge()
        {
            WriteHostJson(new byte[] { 0xEF, 0xBB, 0xBF }.Concat(JsonOfLength(Limit + 1)).ToArray());

            Assert.Equal(HostConfigurationStatus.TooLarge, Get(PhysicalProvider()).Status);
        }

        public static IEnumerable<object[]> UnsupportedEncodings() => new[]
        {
            new object[] { new UnicodeEncoding(false, true) },
            new object[] { new UnicodeEncoding(true, true) },
            new object[] { new UTF32Encoding(false, true) },
            new object[] { new UTF32Encoding(true, true) },
        };

        [Theory]
        [MemberData(nameof(UnsupportedEncodings))]
        public void NonUtf8Sources_AreRejectedWithSafeDiagnostic(Encoding encoding)
        {
            const string text = "{\"secret\":\"customer-value\"}";
            WriteHostJson(encoding.GetPreamble().Concat(encoding.GetBytes(text)).ToArray());
            var logger = new CapturingLogger();

            var result = Get(PhysicalProvider(logger));

            Assert.Equal(HostConfigurationStatus.Error, result.Status);
            Assert.True(result.Content.IsEmpty);
            string message = Assert.Single(logger.Messages);
            Assert.Contains("InvalidUtf8", message);
            Assert.DoesNotContain("customer-value", message);
            Assert.DoesNotContain(_appDirectory, message);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Utf8Sources_PreserveUnicodeValues(bool emitBom)
        {
            const string value = "h\u00e9llo \u20ac \U0001F600 \uFFFD \uFEFF";
            var encoding = new UTF8Encoding(emitBom, throwOnInvalidBytes: true);
            WriteHostJson(encoding.GetPreamble().Concat(encoding.GetBytes("{\"a\":\"" + value + "\"}")).ToArray());

            var result = Get(PhysicalProvider());

            Assert.Equal(HostConfigurationStatus.Available, result.Status);
            using var document = JsonDocument.Parse(result.Content);
            Assert.Equal(value, document.RootElement.GetProperty("a").GetString());
        }

        [Fact]
        public void MultibyteSource_IsMeasuredInBytesBeforeNormalization()
        {
            string source = "/*" + new string('\u20ac', (Limit - 6) / 3) + "*/{}";
            byte[] exact = Encoding.UTF8.GetBytes(source + new string(' ', Limit - Encoding.UTF8.GetByteCount(source)));
            Assert.Equal(Limit, exact.Length);
            WriteHostJson(exact);

            Assert.Equal("{}", Encoding.UTF8.GetString(Get(PhysicalProvider()).Content.Span));
            WriteHostJson(exact.Concat(new byte[] { (byte)' ' }).ToArray());
            Assert.Equal(HostConfigurationStatus.TooLarge, Get(PhysicalProvider()).Status);
        }

        [Fact]
        public void NoBomSource_IsTreatedAsUtf8()
        {
            byte[] bytes = new UTF8Encoding(false).GetBytes("{\"k\":\"\u00fc\"}");
            WriteHostJson(bytes);

            using var document = JsonDocument.Parse(Get(PhysicalProvider()).Content);
            Assert.Equal("\u00fc", document.RootElement.GetProperty("k").GetString());
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

            Assert.Equal(HostConfigurationStatus.Error, result.Status);
            Assert.True(result.Content.IsEmpty);
        }

        [Fact]
        public void EmptyFile_IsInvalidJsonError_NotMissing()
        {
            WriteHostJson(Array.Empty<byte>());

            var result = Get(PhysicalProvider());

            Assert.Equal(HostConfigurationStatus.Error, result.Status);
            Assert.True(result.Content.IsEmpty);
        }

        [Fact]
        public void BomOnlyFile_IsInvalidJsonError()
        {
            WriteHostJson(new byte[] { 0xEF, 0xBB, 0xBF });

            var result = Get(PhysicalProvider());

            Assert.Equal(HostConfigurationStatus.Error, result.Status);
            Assert.True(result.Content.IsEmpty);
        }

        [Theory]
        [InlineData("[]")]
        [InlineData("[7]")]
        [InlineData("\"s\"")]
        [InlineData("1")]
        [InlineData("null")]
        [InlineData("true")]
        [InlineData("// c\n[1]")]
        public void NonObjectRoot_IsInvalidJsonError(string json)
        {
            WriteHostJson(Encoding.UTF8.GetBytes(json));

            var result = Get(PhysicalProvider());

            Assert.Equal(HostConfigurationStatus.Error, result.Status);
            Assert.True(result.Content.IsEmpty);
        }

        [Fact]
        public void ConvertedPayload_MustBeJson_OtherwiseContentFreeError()
        {
            WriteHostJson(Encoding.UTF8.GetBytes("not json"));

            var result = Get(PhysicalProvider());

            Assert.Equal(HostConfigurationStatus.Error, result.Status);
            Assert.True(result.Content.IsEmpty);
        }

        [Fact]
        public void CommentsAndTrailingCommas_AreNormalizedToStrictCompactJson()
        {
            WriteHostJson(new byte[] { 0xEF, 0xBB, 0xBF }.Concat(Encoding.UTF8.GetBytes(
                "// header\n{ \"version\": \"2.0\", /* inline */ \"logging\": { \"a\": [1, 2,], }, }")).ToArray());

            var result = Get(PhysicalProvider());

            Assert.Equal(HostConfigurationStatus.Available, result.Status);
            Assert.Equal("{\"version\":\"2.0\",\"logging\":{\"a\":[1,2]}}", Encoding.UTF8.GetString(result.Content.Span));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public void NormalizedPayload_IsCappedAfterEscaping(int extraBytes)
        {
            var expectedStatus = extraBytes == 0 ? HostConfigurationStatus.Available : HostConfigurationStatus.TooLarge;
            // STJ writes '&' as the six-byte escape \u0026.
            int escapedCharacters = (Limit - 8) / 6;
            string value = new string('&', escapedCharacters) + new string('a', (Limit - 8) % 6 + extraBytes);
            byte[] source = Encoding.UTF8.GetBytes("{\"k\":\"" + value + "\"}");
            Assert.True(source.Length < Limit);
            WriteHostJson(source);

            var result = Get(PhysicalProvider());

            Assert.Equal(expectedStatus, result.Status);
            if (expectedStatus == HostConfigurationStatus.Available)
            {
                Assert.Equal(Limit, result.Content.Length);
            }
            else
            {
                Assert.True(result.Content.IsEmpty);
            }
        }

        [Fact]
        public void Normalization_PreservesValuesNumbersAndEmptyContainers()
        {
            const string json = "{\"a\":\"x\\u0041\\n\",\"n\":[1e5,-0.50,true,false,null,{},[]],\"e\":{}}";
            WriteHostJson(Encoding.UTF8.GetBytes(json.Replace(",\"e\"", ", // c\n\"e\"") + " "));

            using var document = JsonDocument.Parse(Get(PhysicalProvider()).Content);
            Assert.Equal("xA\n", document.RootElement.GetProperty("a").GetString());
            Assert.Equal("[1e5,-0.50,true,false,null,{},[]]", document.RootElement.GetProperty("n").GetRawText());
            Assert.Equal("{}", document.RootElement.GetProperty("e").GetRawText());
        }

        [Fact]
        public void ExcessiveNesting_IsContentFreeError()
        {
            WriteHostJson(Encoding.UTF8.GetBytes("{\"a\":" + new string('[', 200) + new string(']', 200) + "}"));

            var result = Get(PhysicalProvider());

            Assert.Equal(HostConfigurationStatus.Error, result.Status);
        }

        [Theory]
        [InlineData("  \n")]
        [InlineData("// comment")]
        [InlineData("/* comment */")]
        [InlineData("{")]
        [InlineData("{} {}")]
        public void InvalidJson_IsError(string json)
        {
            WriteHostJson(Encoding.UTF8.GetBytes(json));

            Assert.Equal(HostConfigurationStatus.Error, Get(PhysicalProvider()).Status);
        }

        [Fact]
        public void FileNotFoundDuringRead_IsError_NotMissing()
        {
            var fs = new FakeFileSystem(() => new FailingStream(new FileNotFoundException()));

            var result = HostConfigurationSnapshotProvider.Read(fs);

            Assert.Equal(HostConfigurationStatus.Error, result.Status);
            Assert.Equal(HostConfigurationFailureReason.ReadFailed, result.Reason);
            Assert.True(result.Content.IsEmpty);
        }

        [Fact]
        public void FileNotFoundDuringDispose_IsError_NotMissing()
        {
            var fs = new FakeFileSystem(() => new FailingDisposeStream());

            var result = HostConfigurationSnapshotProvider.Read(fs);

            Assert.Equal(HostConfigurationStatus.Error, result.Status);
            Assert.Equal(HostConfigurationFailureReason.ReadFailed, result.Reason);
            Assert.True(result.Content.IsEmpty);
        }

        [Fact]
        public void BomSplitAcrossReads_IsDetected()
        {
            byte[] bytes = new UTF8Encoding(true).GetPreamble().Concat(Encoding.UTF8.GetBytes("{\"a\":1}")).ToArray();
            var fs = new FakeFileSystem(() => new TrickleStream(bytes, int.MaxValue));

            var result = HostConfigurationSnapshotProvider.Read(fs);

            Assert.Equal("{\"a\":1}", Encoding.UTF8.GetString(result.Content.Span));
        }

        [Theory]
        [InlineData(typeof(UnauthorizedAccessException), "AccessDenied")]
        [InlineData(typeof(DirectoryNotFoundException), "OpenFailed")]
        [InlineData(typeof(IOException), "OpenFailed")]
        public void OpenFailures_HaveDistinctReasons(Type exceptionType, string expected)
        {
            var innerException = (Exception)Activator.CreateInstance(exceptionType);
            var reason = Enum.Parse<HostConfigurationFailureReason>(expected);
            var fs = new FakeFileSystem(() => throw new HostConfigurationFileException(reason, innerException));

            Assert.Equal(expected, HostConfigurationSnapshotProvider.Read(fs).Reason.ToString());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void PhysicalOpenRead_MissingPath_PreservesReasonAndInnerException(bool missingDirectory)
        {
            string path = missingDirectory
                ? Path.Combine(_appDirectory, "missing-directory", "host.json")
                : Path.Combine(_appDirectory, "host.json");
            var fs = new PhysicalHostConfigurationFileSystem();

            var exception = Assert.Throws<HostConfigurationFileException>(() => fs.OpenRead(path));

            if (missingDirectory)
            {
                Assert.Equal(HostConfigurationFailureReason.OpenFailed, exception.Reason);
                Assert.IsType<DirectoryNotFoundException>(exception.InnerException);
            }
            else
            {
                Assert.Equal(HostConfigurationFailureReason.FileNotFound, exception.Reason);
                Assert.IsType<FileNotFoundException>(exception.InnerException);
            }

            Assert.DoesNotContain(_appDirectory, exception.Message);
        }

        [Fact]
        public void PhysicalOpenRead_AccessDenied_PreservesReasonAndInnerException()
        {
            string path = Path.Combine(_appDirectory, "host.json");
            Directory.CreateDirectory(path);
            var fs = new PhysicalHostConfigurationFileSystem();

            var exception = Assert.Throws<HostConfigurationFileException>(() => fs.OpenRead(path));

            Assert.Equal(HostConfigurationFailureReason.AccessDenied, exception.Reason);
            Assert.IsType<UnauthorizedAccessException>(exception.InnerException);
            Assert.DoesNotContain(path, exception.Message);
        }

        [Fact]
        public void PhysicalOpenRead_InvalidPath_PreservesReasonAndInnerException()
        {
            var fs = new PhysicalHostConfigurationFileSystem();

            var exception = Assert.Throws<HostConfigurationFileException>(() => fs.OpenRead("secret-path\0"));

            Assert.Equal(HostConfigurationFailureReason.OpenFailed, exception.Reason);
            Assert.IsType<ArgumentException>(exception.InnerException);
            Assert.DoesNotContain("secret-path", exception.Message);
        }

        [Fact]
        public void ReadFailureReasons_AreInvalidUtf8InvalidJsonAndReadFailed()
        {
            string Reason(byte[] bytes)
                => HostConfigurationSnapshotProvider.Read(new FakeFileSystem(() => new MemoryStream(bytes))).Reason.ToString();

            Assert.Equal("InvalidUtf8", Reason(new byte[] { (byte)'{', 0xFF, (byte)'}' }));
            Assert.Equal("InvalidUtf8", Reason(new byte[] { 0xFE, 0xFF, 0xDC, 0x00 }));
            Assert.Equal("InvalidJson", Reason(Encoding.UTF8.GetBytes("{")));
            Assert.Equal(
                "ReadFailed",
                HostConfigurationSnapshotProvider.Read(new FakeFileSystem(() => new FailingStream())).Reason.ToString());
        }

        [Fact]
        public void CachedFailure_IsLoggedOnceWithFixedReasonOnly()
        {
            var logger = new CapturingLogger();
            var fs = new FakeFileSystem(() => new FailingStream());
            var provider = new HostConfigurationSnapshotProvider(fs, logger);

            Get(provider);
            Get(provider);

            string message = Assert.Single(logger.Messages);
            Assert.Contains("ReadFailed", message);
            Assert.DoesNotContain("secret", message);
            Assert.DoesNotContain("disk error", message);
        }

        [Fact]
        public void CachedOpenFailure_PreservesInnerExceptionWithoutLoggingIt()
        {
            const string secret = "secret-path C:\\customer\\app";
            var innerException = new IOException(secret);
            var exception = new HostConfigurationFileException(HostConfigurationFailureReason.OpenFailed, innerException);
            var logger = new CapturingLogger();
            var fs = new FakeFileSystem(() => throw exception);
            var provider = new HostConfigurationSnapshotProvider(fs, logger);

            var first = Get(provider);
            var second = Get(provider);

            Assert.Same(innerException, exception.InnerException);
            Assert.Equal(1, fs.Opens);
            Assert.Equal(HostConfigurationStatus.Error, first.Status);
            Assert.Equal(HostConfigurationStatus.Error, second.Status);
            Assert.True(first.Content.IsEmpty);
            Assert.True(second.Content.IsEmpty);
            string message = Assert.Single(logger.Messages);
            Assert.Contains("OpenFailed", message);
            Assert.DoesNotContain(secret, message);
            Assert.Null(Assert.Single(logger.Exceptions));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void SuccessAndMissing_AreNotLoggedAsFailures(bool exists)
        {
            if (exists)
            {
                WriteHostJson(Encoding.UTF8.GetBytes("{}"));
            }

            var logger = new CapturingLogger();
            var provider = PhysicalProvider(logger);

            Get(provider);

            Assert.Empty(logger.Messages);
        }

        [Fact]
        public void MissingFile_WithValidAppRoot_IsMissing()
        {
            Assert.Equal(HostConfigurationStatus.Missing, Get(PhysicalProvider()).Status);
        }

        [Fact]
        public void Construction_DoesNotOpenFile()
        {
            var fs = new FakeFileSystem(() => new TrickleStream(new byte[] { 1 }, int.MaxValue));
            var provider = new HostConfigurationSnapshotProvider(fs);

            Assert.Equal(0, fs.Opens);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" \t")]
        [InlineData("configured-application")]
        public void AppRootUsesResolvedApplicationDirectory(string configuredDirectory)
        {
            using TestScopedEnvironmentVariable environment = new("FUNCTIONS_APPLICATION_DIRECTORY", configuredDirectory);
            ApplicationDirectory.ResetForTests();
            string opened = null;
            var fs = new FakeFileSystem(path =>
            {
                opened = path;
                return new MemoryStream(Encoding.UTF8.GetBytes("{}"));
            });
            var provider = new HostConfigurationSnapshotProvider(fs);
            Assert.Equal(HostConfigurationStatus.Available, provider.GetSnapshot().Status);
            string expectedDirectory = string.IsNullOrWhiteSpace(configuredDirectory) ? AppContext.BaseDirectory : configuredDirectory;
            Assert.Equal(Path.Combine(expectedDirectory, "host.json"), opened);
        }

        [Fact]
        public void MissingConfiguredHostJsonDoesNotFallBackToBaseDirectory()
        {
            WriteHostJson(Encoding.UTF8.GetBytes("{}"));
            string configuredDirectory = Path.Combine(_appDirectory, "configured");
            Directory.CreateDirectory(configuredDirectory);
            using TestScopedAppContextBaseDirectory application = new(_appDirectory);
            using TestScopedEnvironmentVariable environment = new("FUNCTIONS_APPLICATION_DIRECTORY", configuredDirectory);
            ApplicationDirectory.ResetForTests();
            var provider = new HostConfigurationSnapshotProvider(new PhysicalHostConfigurationFileSystem());

            var result = provider.GetSnapshot();

            Assert.Equal(HostConfigurationStatus.Missing, result.Status);
            Assert.True(result.Content.IsEmpty);
        }

        [Fact]
        public void HostJsonIsADirectory_IsReadError()
        {
            Directory.CreateDirectory(Path.Combine(_appDirectory, "host.json"));

            Assert.Equal(HostConfigurationStatus.Error, Get(PhysicalProvider()).Status);
        }

        [Fact]
        public void ExclusivelyLockedFile_IsReadError()
        {
            string path = Path.Combine(_appDirectory, "host.json");
            File.WriteAllText(path, "{}");
            using var locked = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

            Assert.Equal(HostConfigurationStatus.Error, Get(PhysicalProvider()).Status);
        }

        [Fact]
        public void ConcurrentWriterSharingReadWrite_CanBeRead()
        {
            string path = Path.Combine(_appDirectory, "host.json");
            using var writer = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            writer.Write(new byte[] { (byte)'{', (byte)'}' }, 0, 2);
            writer.Flush();

            var result = Get(PhysicalProvider());

            Assert.Equal(HostConfigurationStatus.Available, result.Status);
            Assert.Equal(2, result.Content.Length);
        }

        [Fact]
        public void SeekableStream_WhoseLengthUnderReportsGrowth_IsStillBounded()
        {
            var fs = new FakeFileSystem(() => new UnderReportingStream(Limit + 100));

            var result = HostConfigurationSnapshotProvider.Read(fs);

            Assert.Equal(HostConfigurationStatus.TooLarge, result.Status);
        }

        [Fact]
        public void NonSeekableStream_ReturningOneByteAtATime_IsReadFully()
        {
            byte[] bytes = Encoding.UTF8.GetBytes("{\"k\":\"" + new string('a', 1000) + "\"}");
            var fs = new FakeFileSystem(() => new TrickleStream(bytes, int.MaxValue));

            var result = HostConfigurationSnapshotProvider.Read(fs);

            Assert.Equal(HostConfigurationStatus.Available, result.Status);
            Assert.Equal(bytes, result.Content.ToArray());
        }

        [Fact]
        public void NonSeekableStream_ExactlyAtLimit_IsAvailable_AndOneMoreIsTooLarge()
        {
            var exact = HostConfigurationSnapshotProvider.Read(new FakeFileSystem(() => new TrickleStream(JsonOfLength(Limit), int.MaxValue)));
            var over = HostConfigurationSnapshotProvider.Read(new FakeFileSystem(() => new TrickleStream(JsonOfLength(Limit + 1), int.MaxValue)));

            Assert.Equal(HostConfigurationStatus.Available, exact.Status);
            Assert.Equal(HostConfigurationStatus.TooLarge, over.Status);
        }

        [Fact]
        public void NeverReadsMuchBeyondLimit_ForUnboundedStream()
        {
            var stream = new TrickleStream(Array.Empty<byte>(), long.MaxValue, endless: true);

            var result = HostConfigurationSnapshotProvider.Read(new FakeFileSystem(() => stream));

            Assert.Equal(HostConfigurationStatus.TooLarge, result.Status);
            Assert.Equal(Limit + 1, stream.TotalServed);
        }

        [Theory]
        [InlineData(typeof(IOException))]
        [InlineData(typeof(UnauthorizedAccessException))]
        [InlineData(typeof(NotSupportedException))]
        public void IoFailureOnOpen_IsContentFreeReadError(Type exceptionType)
        {
            var innerException = (Exception)Activator.CreateInstance(exceptionType, "secret-path C:\\customer\\app");
            var fs = new FakeFileSystem(() => throw new HostConfigurationFileException(HostConfigurationFailureReason.OpenFailed, innerException));

            var result = HostConfigurationSnapshotProvider.Read(fs);

            Assert.Equal(HostConfigurationStatus.Error, result.Status);
            Assert.True(result.Content.IsEmpty);
        }

        [Fact]
        public void IoFailureMidRead_IsReadError()
        {
            var fs = new FakeFileSystem(() => new FailingStream());

            Assert.Equal(HostConfigurationStatus.Error, HostConfigurationSnapshotProvider.Read(fs).Status);
        }

        [Fact]
        public void FileNotFoundOnOpen_IsMissing_AndDirectoryNotFoundIsReadError()
        {
            var missing = new FakeFileSystem(() => throw new HostConfigurationFileException(
                HostConfigurationFailureReason.FileNotFound, new FileNotFoundException()));
            var racedAway = new FakeFileSystem(() => throw new HostConfigurationFileException(
                HostConfigurationFailureReason.OpenFailed, new DirectoryNotFoundException()));

            Assert.Equal(HostConfigurationStatus.Missing, HostConfigurationSnapshotProvider.Read(missing).Status);
            Assert.Equal(HostConfigurationStatus.Error, HostConfigurationSnapshotProvider.Read(racedAway).Status);
        }

        [Fact]
        public void StreamIsDisposed_AfterRead()
        {
            var stream = new TrickleStream(new byte[] { 1 }, int.MaxValue);
            HostConfigurationSnapshotProvider.Read(new FakeFileSystem(() => stream));

            Assert.True(stream.Disposed);
        }

        [Fact]
        public void RepeatedAccess_ReplaysSameSnapshot_WithSingleRead()
        {
            var fs = new FakeFileSystem(() => new TrickleStream(new byte[] { (byte)'{', (byte)'}' }, int.MaxValue));
            var provider = new HostConfigurationSnapshotProvider(fs);

            var first = provider.GetSnapshot();
            var second = provider.GetSnapshot();

            Assert.Equal(1, fs.Opens);
            Assert.Same(first, second);
            Assert.Equal(first.Content, second.Content);
            Assert.Equal(HostConfigurationStatus.Available, second.Status);
        }

        [Fact]
        public void FailedSnapshot_IsCached_UntilProcessRestart()
        {
            int attempt = 0;
            var fs = new FakeFileSystem(() => attempt++ == 0
                ? throw new HostConfigurationFileException(HostConfigurationFailureReason.OpenFailed, new IOException())
                : new TrickleStream(new byte[] { 1 }, int.MaxValue));
            var provider = new HostConfigurationSnapshotProvider(fs);

            var first = provider.GetSnapshot();
            var second = provider.GetSnapshot();

            Assert.Equal(HostConfigurationStatus.Error, first.Status);
            Assert.Equal(HostConfigurationStatus.Error, second.Status);
            Assert.Equal(1, fs.Opens);
        }

        [Fact]
        public void FileChangesAfterFirstAccess_AreNotObserved()
        {
            WriteHostJson(new[] { (byte)'{', (byte)'}' });
            var provider = PhysicalProvider();
            Get(provider);

            WriteHostJson(Encoding.UTF8.GetBytes("{\"a\":1}"));

            Assert.Equal(new[] { (byte)'{', (byte)'}' }, Get(provider).Content.ToArray());
        }

        [Fact]
        public async Task ConcurrentAccess_ReadsOnce_AndAllCallersSeeSameSnapshot()
        {
            var gate = new ManualResetEventSlim();
            var fs = new FakeFileSystem(() =>
            {
                gate.Wait();
                return new TrickleStream(new byte[] { (byte)'{', (byte)'}' }, int.MaxValue);
            });
            var provider = new HostConfigurationSnapshotProvider(fs);

            Task<HostConfigurationSnapshot>[] tasks = Enumerable.Range(0, 16)
                .Select(_ => Task.Run(() => provider.GetSnapshot()))
                .ToArray();
            await Task.Delay(100);
            gate.Set();
            var results = await Task.WhenAll(tasks);

            Assert.Equal(1, fs.Opens);
            Assert.All(results, r => Assert.Same(results[0], r));
            Assert.All(results, r => Assert.Equal(new byte[] { (byte)'{', (byte)'}' }, r.Content.ToArray()));
        }
    }
}
