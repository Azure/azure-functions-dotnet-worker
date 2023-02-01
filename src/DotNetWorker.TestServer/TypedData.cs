using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;

namespace Microsoft.Azure.Functions.Worker.TestServer;

public record TypedData
{
    public string String { get; }
    public string Json { get; }
    public ByteString Bytes { get; }
    public ByteString Stream { get; }
    public Http? Http { get; }
    public long Int { get; }
    public double Double { get; }
    public IReadOnlyCollection<ByteString>? CollectionBytes { get; }
    public IReadOnlyCollection<string>? CollectionString { get; }
    public IReadOnlyCollection<double>? CollectionDouble { get; }
    public IReadOnlyCollection<long>? CollectionSint64 { get; }
    public ModelBindingData? ModelBindingData { get; }
    public IReadOnlyCollection<ModelBindingData>? CollectionModelBindingData { get; }

    private TypedData(string @string, string json, ByteString bytes, ByteString stream, Http? http, long @int, double @double, IReadOnlyCollection<ByteString>? collectionBytes, IReadOnlyCollection<string>? collectionString, IReadOnlyCollection<double>? collectionDouble, IReadOnlyCollection<long>? collectionSint64, ModelBindingData? modelBindingData, IReadOnlyCollection<ModelBindingData>? collectionModelBindingData)
    {
        String = @string;
        Json = json;
        Bytes = bytes;
        Stream = stream;
        Http = http;
        Int = @int;
        Double = @double;
        CollectionBytes = collectionBytes;
        CollectionString = collectionString;
        CollectionDouble = collectionDouble;
        CollectionSint64 = collectionSint64;
        ModelBindingData = modelBindingData;
        CollectionModelBindingData = collectionModelBindingData;
    }

    internal static TypedData? From(Grpc.Messages.TypedData? typedData)
    {
        if (typedData == null) return null;
        return new TypedData(typedData.String, typedData.Json, typedData.Bytes, typedData.Stream,
            Http.From(typedData.Http), typedData.Int, typedData.Double, typedData.CollectionBytes?.Bytes.Clone(),
            typedData.CollectionString?.String.Clone(), typedData.CollectionDouble?.Double.Clone(), typedData.CollectionSint64?.Sint64.Clone(),
            ModelBindingData.From(typedData.ModelBindingData),
            typedData.CollectionModelBindingData?.ModelBindingData?.Select(ModelBindingData.From).ToArray());
    }
}
