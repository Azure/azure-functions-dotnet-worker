// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Buffers.Text;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core.Serialization;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Functions.Worker.Converters
{
    internal class JsonPocoConverter : IInputConverter
    {
        private readonly ObjectSerializer _serializer;

        public JsonPocoConverter(IOptions<WorkerOptions> options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            if (options.Value.Serializer == null)
            {
                throw new InvalidOperationException(nameof(options.Value.Serializer));
            }

            _serializer = options.Value.Serializer;
        }

        public async ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
        {
            if (context.TargetType == typeof(string))
            {
                return ConversionResult.Unhandled();
            }

            if (context.Source is string sourceString)
            {
                if (TryParseIntegralPrimitive(context.TargetType, sourceString, out var converted))
                {
                    return ConversionResult.Success(converted);
                }

                var bytesFromString = Encoding.UTF8.GetBytes(sourceString);
                return await GetConversionResultFromDeserialization(bytesFromString, context.TargetType);
            }

            if (context.Source is ReadOnlyMemory<byte> sourceMemory)
            {
                if (TryParseIntegralPrimitive(context.TargetType, sourceMemory.Span, out var converted))
                {
                    return ConversionResult.Success(converted);
                }

                if (MemoryMarshal.TryGetArray(sourceMemory, out ArraySegment<byte> segment) && segment.Array != null)
                {
                    return await GetConversionResultFromDeserialization(segment.Array, segment.Offset, segment.Count, context.TargetType);
                }

                var bytes = sourceMemory.ToArray();
                return await GetConversionResultFromDeserialization(bytes, context.TargetType);
            }

            return ConversionResult.Unhandled();
        }

        private static bool TryParseIntegralPrimitive(Type t, string value, out object? result)
        {
            result = null;

            var targetType = Nullable.GetUnderlyingType(t) ?? t;

            if (targetType == typeof(int) &&
                int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
            {
                result = i;
                return true;
            }
            if (targetType == typeof(long) &&
                long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l))
            {
                result = l;
                return true;
            }
            if (targetType == typeof(short) &&
                short.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var s))
            {
                result = s;
                return true;
            }
            if (targetType == typeof(byte) &&
                byte.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var b))
            {
                result = b;
                return true;
            }
            if (targetType == typeof(uint) &&
                uint.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ui))
            {
                result = ui;
                return true;
            }
            if (targetType == typeof(ulong) &&
                ulong.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ul))
            {
                result = ul;
                return true;
            }
            if (targetType == typeof(ushort) &&
                ushort.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var us))
            {
                result = us;
                return true;
            }
            if (targetType == typeof(sbyte) &&
                sbyte.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var sb))
            {
                result = sb;
                return true;
            }

            return false;
        }

        private static bool TryParseIntegerPrimitive(Type t, ReadOnlySpan<byte> utf8, out object? result)
        {
            result = null;

            var targetType = Nullable.GetUnderlyingType(t) ?? t;

            if (targetType == typeof(int) &&
                Utf8Parser.TryParse(utf8, out int i, out int consumed) && consumed == utf8.Length)
            {
                result = i;
                return true;
            }
            if (targetType == typeof(long) &&
                Utf8Parser.TryParse(utf8, out long l, out consumed) && consumed == utf8.Length)
            {
                result = l;
                return true;
            }
            if (targetType == typeof(short) &&
                Utf8Parser.TryParse(utf8, out short s, out consumed) && consumed == utf8.Length)
            {
                result = s;
                return true;
            }
            if (targetType == typeof(byte) &&
                Utf8Parser.TryParse(utf8, out byte b, out consumed) && consumed == utf8.Length)
            {
                result = b;
                return true;
            }
            if (targetType == typeof(uint) &&
                Utf8Parser.TryParse(utf8, out uint ui, out consumed) && consumed == utf8.Length)
            {
                result = ui;
                return true;
            }
            if (targetType == typeof(ulong) &&
                Utf8Parser.TryParse(utf8, out ulong ul, out consumed) && consumed == utf8.Length)
            {
                result = ul;
                return true;
            }
            if (targetType == typeof(ushort) &&
                Utf8Parser.TryParse(utf8, out ushort us, out consumed) && consumed == utf8.Length)
            {
                result = us;
                return true;
            }
            if (targetType == typeof(sbyte) &&
                Utf8Parser.TryParse(utf8, out sbyte sb, out consumed) && consumed == utf8.Length)
            {
                result = sb;
                return true;
            }

            return false;
        }

        private Task<ConversionResult> GetConversionResultFromDeserialization(byte[] bytes, Type type)
            => GetConversionResultFromDeserialization(bytes, 0, bytes.Length, type);

        private async Task<ConversionResult> GetConversionResultFromDeserialization(byte[] bytes, int offset, int count, Type type)
        {
            Stream? stream = null;

            try
            {
                stream = new MemoryStream(bytes, offset, count, writable: false, publiclyVisible: true);

                var deserializedObject = await _serializer.DeserializeAsync(stream, type, CancellationToken.None);
                return ConversionResult.Success(deserializedObject);
            }
            catch (Exception ex)
            {
                return ConversionResult.Failed(ex);
            }
            finally
            {
                if (stream != null)
                {
#if NET6_0_OR_GREATER
                    await ((IAsyncDisposable)stream).DisposeAsync();
#else
                    ((IDisposable)stream).Dispose();
#endif
                }
            }
        }
    }
}
