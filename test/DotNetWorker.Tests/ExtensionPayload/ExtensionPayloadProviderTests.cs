// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Grpc;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests;

public sealed class ExtensionPayloadProviderTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(17)]
    [InlineData(4096)]
    public async Task ManifestBufferMatchesStableInputLength(int size)
    {
        byte[] content = new byte[size];
        new Random(23).NextBytes(content);
        using SizedReadStream input = new(content, size);
        (byte[] bytes, int length) = await ExtensionPayloadProvider.ReadManifestAsync(input, CancellationToken.None);

        Assert.Equal(size + 1, bytes.Length);
        Assert.Equal(size + 1, input.FirstBufferSize);
        Assert.Equal(size, length);
        Assert.Equal(content, bytes.AsSpan(0, length).ToArray());
        Assert.Equal(1, input.LengthReads);
        Assert.True(input.CanRead);
    }

    [Theory]
    [InlineData(0, 37, 8)]
    [InlineData(2, 1024, 17)]
    [InlineData(3, 1048576, 8191)]
    [InlineData(1024, 16, 5)]
    public async Task ManifestReadsActualContentsDespiteLengthChangesAndShortReads(int reportedLength, int actualLength, int readSize)
    {
        byte[] content = new byte[actualLength];
        new Random(23).NextBytes(content);
        using SizedReadStream input = new(content, reportedLength, readSize);
        (byte[] bytes, int length) = await ExtensionPayloadProvider.ReadManifestAsync(input, CancellationToken.None);

        Assert.Equal(reportedLength + 1, input.FirstBufferSize);
        Assert.Equal(actualLength, length);
        Assert.Equal(content, bytes.AsSpan(0, length).ToArray());
        Assert.InRange(bytes.Length, actualLength + 1, ExtensionPayloadProvider.MaximumManifestBytes + 1);
        Assert.Equal(bytes.Length, input.MaximumBufferSize);
        Assert.Equal(1, input.LengthReads);
        Assert.True(input.CanRead);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1048576)]
    public async Task ManifestGrowthBeyondLimitIsRejectedAfterOneExtraByte(int reportedLength)
    {
        using SizedReadStream input = new(new byte[ExtensionPayloadProvider.MaximumManifestBytes + 1], reportedLength);
        await Assert.ThrowsAsync<InvalidDataException>(() => ExtensionPayloadProvider.ReadManifestAsync(input, CancellationToken.None));

        Assert.Equal(reportedLength + 1, input.FirstBufferSize);
        Assert.Equal(ExtensionPayloadProvider.MaximumManifestBytes + 1, input.Position);
        Assert.InRange(input.MaximumBufferSize, 1, ExtensionPayloadProvider.MaximumManifestBytes + 1);
    }

    [Theory]
    [InlineData(1048577L)]
    [InlineData(long.MaxValue)]
    public async Task OversizedInitialManifestIsRejectedBeforeAllocationOrReading(long reportedLength)
    {
        using SizedReadStream input = new([], reportedLength);
        await Assert.ThrowsAsync<InvalidDataException>(() => ExtensionPayloadProvider.ReadManifestAsync(input, CancellationToken.None));
        Assert.Equal(0, input.ReadCount);
        Assert.Equal(1, input.LengthReads);
    }

    [Fact]
    public async Task CancelledManifestReadDoesNotAllocateOrReadInput()
    {
        using SizedReadStream input = new([], 0);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            ExtensionPayloadProvider.ReadManifestAsync(input, new CancellationToken(canceled: true)));
        Assert.Equal(0, input.ReadCount);
        Assert.Equal(0, input.LengthReads);
    }

    [Fact]
    public async Task ManifestGrowthHonorsCancellationBetweenReads()
    {
        using CancellationTokenSource cancellation = new();
        using SizedReadStream input = new(new byte[64], 0);
        input.BeforeRead = read =>
        {
            if (read == 2)
            {
                cancellation.Cancel();
            }
        };
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            ExtensionPayloadProvider.ReadManifestAsync(input, cancellation.Token));
        Assert.Equal(2, input.ReadCount);
        Assert.Equal(1, input.Position);
        Assert.True(input.CanRead);
    }

    private sealed class SizedReadStream(byte[] content, long reportedLength, int readSize = int.MaxValue)
        : MemoryStream(content, writable: false)
    {
        public int LengthReads { get; private set; }
        public int ReadCount { get; private set; }
        public int FirstBufferSize { get; private set; }
        public int MaximumBufferSize { get; private set; }
        public Action<int> BeforeRead { get; set; }

        public override long Length
        {
            get
            {
                LengthReads++;
                return reportedLength;
            }
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            Assert.InRange(count, 1, buffer.Length - offset);
            Assert.InRange(buffer.Length, 1, ExtensionPayloadProvider.MaximumManifestBytes + 1);
            if (++ReadCount == 1)
            {
                FirstBufferSize = buffer.Length;
            }

            MaximumBufferSize = Math.Max(MaximumBufferSize, buffer.Length);
            BeforeRead?.Invoke(ReadCount);
            return base.ReadAsync(buffer, offset, Math.Min(count, readSize), cancellationToken);
        }
    }
}
