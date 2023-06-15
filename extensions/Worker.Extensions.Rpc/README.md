# Worker.Extensions.Rpc

This package provides helpers for RPC communication with the functions host.

## Commonly used types

- `FunctionsGrpcOptions`
- `GrpcHttpClientBuilderExtensions.ConfigureForFunctionsHostGrpc` (>= net6.0 only)

## Example usage

Both examples below you have some gRPC client "`MyGrpcClient`" which has a `CallInvoker` accepting constructor.

### netstandard

When using netstandard2.0, creating a gRPC client that communicates to the host is done via `FunctionsGrpcOptions`.

``` CSharp
[assembly: WorkerExtensionStartup(typeof(WorkerRpcStartup))]
public class MyWorkerExtensionStartup : WorkerExtensionStartup
{
    public override void Configure(IFunctionsWorkerApplicationBuilder applicationBuilder)
    {
        applicationBuilder.Services.AddTransient<MyGrpcClient>(sp =>
        {
            IOptions<FunctionsGrpcOptions> options = sp.GetRequiredService<IOptions<FunctionsGrpcOptions>>();
            return new MyGrpcClient(options.CallInvoker);
        });
    }
}
```

### net6.0

When using net6.0 or newer, creating a gRPC client can be done either the same way as the [netstandard](#netstandard) example, or it can be done via [Grpc.Net.ClientFactory](https://learn.microsoft.com/aspnet/core/grpc/clientfactory). Using the client factory lets you further customize your client.

``` CSharp
[assembly: WorkerExtensionStartup(typeof(WorkerRpcStartup))]
public class MyWorkerExtensionStartup : WorkerExtensionStartup
{
    public override void Configure(IFunctionsWorkerApplicationBuilder applicationBuilder)
    {
        applicationBuilder.Services
            .AddGrpcClient<MyGrpcClient>(options =>
            {
                // configure options here as necessary.
            })
            .ConfigureForFunctionsHostGrpc();
    }
}
```
