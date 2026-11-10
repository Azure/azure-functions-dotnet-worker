// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;

namespace Microsoft.Azure.Functions.Worker.Core.Configuration
{
    /// <summary>
    /// Reads a bounded UTF-8 source into a validated, BOM-free stream for JSON parsing.
    /// Other file encodings are not supported. The caller owns both streams.
    /// </summary>
    internal static class HostConfigurationReader
    {
        internal const int MaxContentBytes = 64 * 1024;

        private static readonly byte[] Utf8Preamble = Encoding.UTF8.GetPreamble();
        private static readonly Encoding StrictUtf8 = new UTF8Encoding(false, throwOnInvalidBytes: true);

        public static Stream Read(Stream source)
        {
            // Allow a BOM and one extra byte to detect overflow without trusting Length.
            byte[] buffer = new byte[MaxContentBytes + Utf8Preamble.Length + 1];
            int count = 0;
            while (count < Utf8Preamble.Length)
            {
                int read = source.Read(buffer, count, Utf8Preamble.Length - count);
                if (read == 0)
                {
                    break;
                }

                count += read;
            }

            int offset = buffer.AsSpan(0, count).StartsWith(Utf8Preamble) ? Utf8Preamble.Length : 0;
            int limit = offset + MaxContentBytes + 1;
            while (count < limit)
            {
                int read = source.Read(buffer, count, Math.Min(4096, limit - count));
                if (read == 0)
                {
                    break;
                }

                count += read;
            }

            int length = count - offset;
            if (length > MaxContentBytes)
            {
                throw new HostConfigurationTooLargeException();
            }

            // STJ can defer decoding string values, so validate all bytes before parsing.
            StrictUtf8.GetCharCount(buffer, offset, length);
            return new MemoryStream(buffer, offset, length, writable: false);
        }
    }
}
