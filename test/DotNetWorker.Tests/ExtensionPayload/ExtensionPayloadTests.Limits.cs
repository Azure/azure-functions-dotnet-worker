// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Tests;
using Microsoft.Azure.Functions.Worker.Grpc;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests;

public sealed partial class ExtensionPayloadTests
{
    [Fact]
    public void EntryLimitAccepts10000AndRejects10001()
    {
        using MemoryStream bytes = new();
        using (ZipArchive archive = new(bytes, ZipArchiveMode.Create, leaveOpen: true))
        {
            int count = 0;
            for (int index = 0; index < 10000; index++)
            {
                ExtensionPayload.AddEntry(archive, index.ToString("D5", CultureInfo.InvariantCulture), ref count);
            }

            Assert.Equal(10000, count);
            Assert.Throws<InvalidDataException>(() => ExtensionPayload.AddEntry(archive, "one-too-many", ref count));
        }

        bytes.Position = 0;
        using ZipArchive result = new(bytes, ZipArchiveMode.Read);
        Assert.Equal(10000, result.Entries.Count);
    }

    [Fact]
    public void ContentAndZipBudgetsAcceptBoundariesAndRejectOneByteOver()
    {
        Assert.Equal(1073741824L, ExtensionPayload.MaximumContentBytes);
        Assert.Equal(268435456L, ExtensionPayload.MaximumArchiveBytes);
        long remaining = ExtensionPayload.MaximumContentBytes - new FileInfo(Path.Combine(_root, "extensions.json")).Length;
        ExtensionPayload.EnsureContentBytesFit(remaining, remaining);
        ExtensionPayload.EnsureContentBytesFit(0, 0);
        Assert.Throws<InvalidDataException>(() => ExtensionPayload.EnsureContentBytesFit(remaining + 1, remaining));
        ExtensionPayload.EnsureArchiveBytesFit(ExtensionPayload.MaximumArchiveBytes - 1, 1);
        ExtensionPayload.EnsureArchiveBytesFit(ExtensionPayload.MaximumArchiveBytes, 0);
        Assert.Throws<InvalidDataException>(() => ExtensionPayload.EnsureArchiveBytesFit(ExtensionPayload.MaximumArchiveBytes, 1));
    }

    [Fact]
    public async Task ManifestLimitAccepts1MiBAndRejectsOneByteOver()
    {
        Assert.Equal(1048576, ExtensionPayload.MaximumManifestBytes);
        File.WriteAllText(Path.Combine(_root, "extensions.json"), Manifest.PadRight(ExtensionPayload.MaximumManifestBytes));
        using (FileStream archive = await ExtensionPayload.CreateArchiveAsync(CancellationToken.None))
        {
            Assert.True(archive.Length > 0);
        }

        File.AppendAllText(Path.Combine(_root, "extensions.json"), " ");
        await Assert.ThrowsAsync<InvalidDataException>(async () =>
        {
            using FileStream archive = await ExtensionPayload.CreateArchiveAsync(CancellationToken.None);
        });
        AssertNoTemporaryArchives();
    }

    [Fact]
    public void PathDepthLimitAccepts32SegmentsAndRejects33()
    {
        using MemoryStream bytes = new();
        using ZipArchive archive = new(bytes, ZipArchiveMode.Create);
        int count = 0;
        ExtensionPayload.AddEntry(archive, string.Join('/', Enumerable.Repeat("a", 32)), ref count);
        Assert.Throws<InvalidDataException>(() => ExtensionPayload.AddEntry(archive, string.Join('/', Enumerable.Repeat("a", 33)), ref count));
    }

    [Fact]
    public async Task PreparationFailureDeletesPartialZip()
    {
        using FileStream held = File.Open(Path.Combine(_root, ".azurefunctions", "locked.bin"), FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);
        Exception failure = await Record.ExceptionAsync(async () =>
        {
            using FileStream archive = await ExtensionPayload.CreateArchiveAsync(CancellationToken.None);
        });
        Assert.NotNull(failure);
        AssertNoTemporaryArchives();
    }

    [RequiresPayloadSymbolicLinks]
    public async Task SkipsNestedLinkedFilesDirectoriesAndCycles()
    {
        string excluded = Path.Combine(_root, "excluded");
        Directory.CreateDirectory(excluded);
        string secret = Path.Combine(excluded, "secret.bin");
        File.WriteAllText(secret, "not an extension payload file");
        string extensions = Path.Combine(_root, ".azurefunctions");
        File.WriteAllText(Path.Combine(extensions, "included.bin"), "included");
        Directory.CreateSymbolicLink(Path.Combine(extensions, "linked"), excluded);
        Directory.CreateSymbolicLink(Path.Combine(extensions, "cycle"), extensions);
        File.CreateSymbolicLink(Path.Combine(extensions, "linked-file"), secret);
        using (FileStream stream = await ExtensionPayload.CreateArchiveAsync(CancellationToken.None))
        using (ZipArchive archive = new(stream, ZipArchiveMode.Read))
        {
            Assert.Equal(3, archive.Entries.Count);
            Assert.NotNull(archive.GetEntry(".azurefunctions/included.bin"));
        }

        AssertNoTemporaryArchives();
    }

    [RequiresPayloadSymbolicLinks]
    public async Task RejectsLinkedPayloadRootBeforeCreatingZip()
    {
        string excluded = Path.Combine(_root, "excluded");
        Directory.CreateDirectory(excluded);
        string extensions = Path.Combine(_root, ".azurefunctions");
        Directory.Delete(extensions);
        Directory.CreateSymbolicLink(extensions, excluded);
        await Assert.ThrowsAsync<InvalidDataException>(async () =>
        {
            using FileStream archive = await ExtensionPayload.CreateArchiveAsync(CancellationToken.None);
        });
        AssertNoTemporaryArchives();
    }

    [Fact]
    public async Task TemporaryArchiveInsidePayloadIsNotArchived()
    {
        string extensions = Path.Combine(_root, ".azurefunctions");
        using TestScopedEnvironmentVariable tmp = new("TMP", extensions);
        using TestScopedEnvironmentVariable temp = new("TEMP", extensions);
        using TestScopedEnvironmentVariable tmpdir = new("TMPDIR", extensions);
        using (FileStream stream = await ExtensionPayload.CreateArchiveAsync(CancellationToken.None))
        using (ZipArchive archive = new(stream, ZipArchiveMode.Read))
        {
            Assert.Equal(2, archive.Entries.Count);
        }

        Assert.Empty(Directory.EnumerateFiles(extensions));
        AssertNoTemporaryArchives();
    }

    [Fact]
    public async Task ConnectionLossCancelsPreparationAndDeletesZip()
    {
        byte[] content = [1, 2, 3];
        string payloadPath = Path.Combine(_root, ".azurefunctions", "payload.bin");
        File.WriteAllBytes(payloadPath, content);
        using RecordingClient client = new();
        TaskCompletionSource<CancellationToken> readStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using PreparationReadStream input = new(content, readStarted, release.Task);
        Task<FileStream> preparation = ExtensionPayload.CreateArchiveAsync(client.ConnectionClosed, path =>
        {
            Assert.Equal(payloadPath, path);
            return input;
        });
        try
        {
            CancellationToken token = await readStarted.Task.WaitAsync(TimeSpan.FromSeconds(15));
            string archive = Assert.Single(Directory.EnumerateFiles(_temporaryRoot));
            Assert.False(preparation.IsCompleted);
            Assert.False(token.IsCancellationRequested);
            await client.StopAsync(CancellationToken.None);
            Assert.True(token.IsCancellationRequested);
            Assert.False(preparation.IsCompleted);
            Assert.True(File.Exists(archive));
        }
        finally
        {
            await client.StopAsync(CancellationToken.None);
            release.TrySetResult(true);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            {
                using FileStream archive = await preparation.WaitAsync(TimeSpan.FromSeconds(15));
            });
        }

        Assert.Empty(client.Messages);
        AssertNoTemporaryArchives();
    }

    private sealed class PreparationReadStream(byte[] content, TaskCompletionSource<CancellationToken> readStarted, Task release)
        : MemoryStream(content, writable: false)
    {
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            readStarted.TrySetResult(cancellationToken);
            await release.ConfigureAwait(false);
            return await base.ReadAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
        }
    }
}

internal sealed class RequiresPayloadSymbolicLinksAttribute : FactAttribute
{
    public RequiresPayloadSymbolicLinksAttribute()
    {
        string probe = Path.Combine(Path.GetTempPath(), "payload-link-probe-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(probe);
            File.WriteAllText(Path.Combine(probe, "target"), string.Empty);
            File.CreateSymbolicLink(Path.Combine(probe, "link"), Path.Combine(probe, "target"));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException or System.Security.SecurityException)
        {
            Skip = $"Symbolic link creation is unavailable ({exception.GetType().Name}).";
        }
        finally
        {
            try
            {
                if (Directory.Exists(probe))
                {
                    Directory.Delete(probe, recursive: true);
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.Security.SecurityException)
            {
                Skip = $"{Skip} Symbolic-link probe cleanup failed ({exception.GetType().Name}).";
            }
        }
    }
}
