// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;
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

            byte[]? bytes = null;

            if (context.Source is string sourceString)
            {
                bytes = Encoding.UTF8.GetBytes(sourceString);
            }
            else if (context.Source is ReadOnlyMemory<byte> sourceMemory)
            {
                bytes = sourceMemory.ToArray();
            }

            if (bytes == null)
            {
                return ConversionResult.Unhandled();
            }

            var result = await GetConversionResultFromDeserialization(bytes, context.TargetType);

            // JSON only accepts lowercase true/false. When deserialization fails on a value that
            // bool.TryParse accepts (which, like ASP.NET Core model binding, ignores case), retry with
            // the lowercase JSON literal so values such as "True" bind. The configured serializer and
            // any registered converters still decide the result; a value that was already the
            // lowercase literal keeps its original failure.
            if (result.Status == ConversionStatus.Failed
                && context.Source is string sourceText
                && (context.TargetType == typeof(bool) || context.TargetType == typeof(bool?))
                && bool.TryParse(sourceText, out bool parsedBool))
            {
                string jsonLiteral = parsedBool ? "true" : "false";

                if (!string.Equals(sourceText.Trim(), jsonLiteral, StringComparison.Ordinal))
                {
                    return await GetConversionResultFromDeserialization(Encoding.UTF8.GetBytes(jsonLiteral), context.TargetType);
                }
            }

            return result;
        }

        private async Task<ConversionResult> GetConversionResultFromDeserialization(byte[] bytes, Type type)
        {
            Stream? stream = null;

            try
            {
                stream = new MemoryStream(bytes);

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
