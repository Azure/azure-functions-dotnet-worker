// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Threading;
using Azure.Core.Serialization;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Functions.Worker.Converters
{
    internal class JsonPocoConverter : IConverter
    {
        private readonly ObjectSerializer _serializer;

        public JsonPocoConverter(IOptions<WorkerOptions> options)
        {
            Guard.AgainstNull(nameof(options), options);

            Guard.AgainstNull(nameof(options.Value.Serializer), options.Value.Serializer);
            
            _serializer = options.Value.Serializer!;
        }

        public bool TryConvert(ConverterContext context, out object? target)
        {
            target = default;

            if (context.Parameter.Type == typeof(string))
            {
                return false;
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
                return false;
            }

            return TryDeserialize(bytes, context.Parameter.Type, out target);
        }

        private bool TryDeserialize(byte[] bytes, Type type, out object? target)
        {
            target = default;

            try
            {
                using (var stream = new MemoryStream(bytes))
                {
                    target = _serializer.Deserialize(stream, type, CancellationToken.None);
                }

                return true;
            }
            catch
            {
                return false;
            }

        }
    }
}
