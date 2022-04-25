using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Azure.Functions.Worker.Extensions.SignalRService
{
    internal class HttpHeaderDictionaryConverter : JsonConverter<IDictionary<string, StringValues>>
    {
        public override IDictionary<string, StringValues> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var dict = JsonSerializer.Deserialize<IDictionary<string, string>>(ref reader, options);
            return dict.ToDictionary(pair => pair.Key,
                pair => pair.Value == null ? default : new StringValues(pair.Value.Split(',')));
        }

        public override void Write(Utf8JsonWriter writer, IDictionary<string, StringValues> value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, options);
        }
    }
}
