using System;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Functions.Worker.Converters
{
    internal class JsonPocoConverter : IConverter
    {
        private readonly IOptions<JsonSerializerOptions> _options;

        public JsonPocoConverter(IOptions<JsonSerializerOptions> options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
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
                target = JsonSerializer.Deserialize(sourceString, context.Parameter.Type, _options.Value);
                return true;
            }
            catch (JsonException)
            {
            }

            return false;
        }
    }
}
