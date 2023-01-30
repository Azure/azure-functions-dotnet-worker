using Microsoft.Azure.Functions.Worker.Grpc.Messages;

namespace Microsoft.Azure.Functions.Worker.TestServer;

public record Exception
{
    public string Type { get; }
    public string Source { get; }
    public string Message { get; }
    public bool IsUserException { get; }
    public string StackTrace { get; }

    private Exception(string type, string source, string message, bool isUserException, string stackTrace)
    {
        Type = type;
        Source = source;
        Message = message;
        IsUserException = isUserException;
        StackTrace = stackTrace;
    }

    internal static Exception? From(RpcException? exception)
    {
        if (exception == null) return null;
        return new Exception(exception.Type, exception.Source, exception.Message, exception.IsUserException, exception.StackTrace);
    }
}
