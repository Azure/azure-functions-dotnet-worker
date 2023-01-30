using System;

namespace Microsoft.Azure.Functions.Worker.TestServer;

public record StatusResult
{
    private static StatusResult? _successInstance;
    public Exception? Exception { get; }
    public Status Status { get; }
    public string Result { get; }

    private StatusResult(Exception? exception, Status status, string result)
    {
        Exception = exception;
        Status = status;
        Result = result;
    }

    internal static StatusResult From(Grpc.Messages.StatusResult statusResult)
    {
        return new StatusResult(Exception.From(statusResult.Exception), Map(statusResult.Status),
            statusResult.Result);
    }

    public static StatusResult Success => _successInstance ??= new StatusResult(null, Status.Success, string.Empty);

    private static Status Map(Grpc.Messages.StatusResult.Types.Status status) =>
        status switch
        {
            Grpc.Messages.StatusResult.Types.Status.Failure => Status.Failure,
            Grpc.Messages.StatusResult.Types.Status.Success => Status.Success,
            Grpc.Messages.StatusResult.Types.Status.Cancelled => Status.Cancelled,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };
}
