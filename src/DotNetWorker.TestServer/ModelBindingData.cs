using Google.Protobuf;

namespace Microsoft.Azure.Functions.Worker.TestServer;

public class ModelBindingData
{
    public ByteString Content { get; }
    public string ContentType { get; }
    public string Source { get; }
    public string Version { get; }

    private ModelBindingData(ByteString content, string contentType, string source, string version)
    {
        Content = content;
        ContentType = contentType;
        Source = source;
        Version = version;
    }

    internal static ModelBindingData? From(
        Grpc.Messages.ModelBindingData? modelBindingData)
    {
        if (modelBindingData == null) return null;
        return new ModelBindingData(modelBindingData.Content, modelBindingData.ContentType, modelBindingData.Source, modelBindingData.Version);
    }
}
