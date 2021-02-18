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
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _serializer = options.Value.Serializer;
        }

        public bool TryConvert(ConverterContext context, out object? target)
        {
            target = default;

            if (context.Parameter.Type == typeof(string) ||
                context.Source is not string sourceString ||
                string.IsNullOrEmpty(sourceString))
            {
                return false;
            }

            try
            {
                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(sourceString)))
                {
                    target = _serializer.Deserialize(stream, context.Parameter.Type, CancellationToken.None);
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
