// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Converters;

namespace Microsoft.Azure.Functions.Worker.Extensions.Timer.Converters
{
    internal sealed class TimerPocoConverter : IInputConverter
    {
        public async ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
        {
            if (context.TargetType != typeof(TimerInfo))
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

            return await GetConversionResultFromDeserialization(bytes);
        }

        private async Task<ConversionResult> GetConversionResultFromDeserialization(byte[] bytes)
        {
            try
            {
                using var stream = new MemoryStream(bytes);
                var deserializedObject = await JsonSerializer.DeserializeAsync<TimerInfo>(stream, JsonSerializerOptionsProvider.Options);
                return ConversionResult.Success(deserializedObject);
            }
            catch (Exception ex)
            {
                return ConversionResult.Failed(ex);
            }
        }
    }
}
