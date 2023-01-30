using Microsoft.Azure.Functions.Worker.Grpc.Messages;

namespace Microsoft.Azure.Functions.Worker.TestServer;

public class Http
{
    public string StatusCode { get; }
    public TypedData Body { get; }
    public TypedData RawBody { get; }

    private Http(string statusCode, TypedData body, TypedData rawBody)
    {
        StatusCode = statusCode;
        Body = body;
        RawBody = rawBody;
    }

    internal static Http? From(
        RpcHttp? http)
    {
        if (http == null) return null;
        return new Http(http.StatusCode, TypedData.From(http.Body), TypedData.From(http.RawBody)); // TODO add missing properties
    }
}
