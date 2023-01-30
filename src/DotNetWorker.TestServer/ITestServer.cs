using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Worker.TestServer;

/// <summary>
/// public interface for the test server
/// </summary>
public interface ITestServer
{
    /// <summary>
    /// Initialize the server
    /// </summary>
    Task StartAsync();

    /// <summary>Calls a function method by the function name.</summary>
    /// <param name="name">The name of the function to call.</param>
    /// <param name="arguments">The argument names and values to bind to parameters in the function. In addition to parameter values, these may also include binding data values. </param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Task{FunctionResponse}"/> that will call the function and give the response of the function.</returns>
    public Task<FunctionResponse> CallAsync(string name, IDictionary<string, object> arguments,
        CancellationToken cancellationToken = default);
}
