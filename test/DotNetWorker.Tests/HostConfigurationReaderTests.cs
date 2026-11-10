// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Azure.Functions.Worker.Core.Configuration;
using Microsoft.Azure.Functions.Worker.Tests.HostConfigurationDelivery;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests
{
    public class HostConfigurationReaderTests
    {
        [Theory]
        [InlineData(false, 1)]
        [InlineData(true, 1)]
        [InlineData(false, 2)]
        [InlineData(true, 2)]
        [InlineData(false, 3)]
        [InlineData(true, 3)]
        [InlineData(false, 7)]
        [InlineData(true, 7)]
        [InlineData(false, 4096)]
        [InlineData(true, 4096)]
        public void Read_ReturnsBomFreeUtf8AcrossChunks_WithoutParsing(bool emitBom, int chunkSize)
        {
            string text = new string('a', 4095) + "\u20ac\U0001F600\uFFFD\uFEFF";
            var encoding = new UTF8Encoding(emitBom, throwOnInvalidBytes: true);
            byte[] expected = encoding.GetBytes(text);
            using var source = new ChunkedStream(encoding.GetPreamble().Concat(expected).ToArray(), chunkSize);

            using (Stream content = HostConfigurationReader.Read(source))
            {
                Assert.Equal(0, content.Position);
                Assert.False(content.CanWrite);
                using var output = new MemoryStream();
                content.CopyTo(output);
                Assert.Equal(expected, output.ToArray());
            }

            Assert.True(source.CanRead);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Read_EnforcesExactLimitExcludingBom(bool emitBom)
        {
            byte[] preamble = new UTF8Encoding(emitBom).GetPreamble();
            byte[] bytes = preamble.Concat(Enumerable.Repeat((byte)'a', HostConfigurationReader.MaxContentBytes)).ToArray();
            using var exact = new ChunkedStream(bytes, 7);
            using var over = new ChunkedStream(bytes.Concat(new byte[] { (byte)'a' }).ToArray(), 7);

            using Stream content = HostConfigurationReader.Read(exact);

            Assert.Equal(HostConfigurationReader.MaxContentBytes, content.Length);
            Assert.Throws<HostConfigurationTooLargeException>(() => HostConfigurationReader.Read(over));
            Assert.Equal(preamble.Length + HostConfigurationReader.MaxContentBytes + 1, over.BytesRead);
        }

        [Theory]
        [InlineData(new byte[] { })]
        [InlineData(new byte[] { (byte)'a' })]
        [InlineData(new byte[] { (byte)'a', (byte)'b' })]
        [InlineData(new byte[] { 0xEF, 0xBB, 0xBF })]
        public void Read_HandlesShortSources(byte[] bytes)
        {
            using var source = new ChunkedStream(bytes, 1);
            using Stream content = HostConfigurationReader.Read(source);

            Assert.Equal(bytes.Length == 3 ? 0 : bytes.Length, content.Length);
        }

        [Theory]
        [InlineData(new byte[] { 0xFF })]
        [InlineData(new byte[] { 0xEF })]
        [InlineData(new byte[] { 0xEF, 0xBB })]
        [InlineData(new byte[] { 0xC0, 0xAF })]
        [InlineData(new byte[] { 0xE2, 0x82 })]
        [InlineData(new byte[] { 0xED, 0xA0, 0x80 })]
        [InlineData(new byte[] { 0xF4, 0x90, 0x80, 0x80 })]
        [InlineData(new byte[] { 0x80 })]
        [InlineData(new byte[] { 0xFF, 0xFE, 0x61, 0x00 })]
        [InlineData(new byte[] { 0xFE, 0xFF, 0x00, 0x61 })]
        [InlineData(new byte[] { 0xFF, 0xFE, 0x00, 0x00, 0x61, 0x00, 0x00, 0x00 })]
        [InlineData(new byte[] { 0x00, 0x00, 0xFE, 0xFF, 0x00, 0x00, 0x00, 0x61 })]
        public void Read_RejectsMalformedOrNonUtf8Bytes(byte[] invalid)
        {
            foreach (bool emitBom in new[] { false, true })
            {
                byte[] preamble = new UTF8Encoding(emitBom).GetPreamble();
                using var source = new ChunkedStream(preamble.Concat(invalid).ToArray(), 1);

                Assert.Throws<DecoderFallbackException>(() => HostConfigurationReader.Read(source));
                Assert.True(source.CanRead);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Read_RejectsTruncatedUtf8AfterReadBoundary(bool emitBom)
        {
            var encoding = new UTF8Encoding(emitBom);
            byte[] complete = encoding.GetPreamble().Concat(encoding.GetBytes(new string('a', 4096) + "\U0001F600")).ToArray();
            using var source = new ChunkedStream(complete.Take(complete.Length - 1).ToArray(), 4096);

            Assert.Throws<DecoderFallbackException>(() => HostConfigurationReader.Read(source));
        }
    }
}
