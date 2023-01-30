namespace Microsoft.Azure.Functions.Worker.TestServer;

/// <summary>
/// The execution status
/// </summary>
public enum Status
{
    /// <summary>
    /// Failure
    /// </summary>
    Failure = 0,
    /// <summary>
    /// Success
    /// </summary>
    Success = 1,
    /// <summary>
    /// Cancelled
    /// </summary>
    Cancelled = 2,
}