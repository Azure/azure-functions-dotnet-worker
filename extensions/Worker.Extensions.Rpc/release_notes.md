## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Worker.Extensions.Rpc 1.0.0-preview1

- Add `IHttpClientBuilder.ConfigureForFunctionsHostGrpc` extension method. (#1616)
  - Used along with `IServiceCollection.AddGrpcClient<TClient>()` to configure for Functions host communication.
  - **NOTE**: when using Grpc.Net.ClientFactory <= 2.54.0-pre1 use `IServiceCollection.AddGrpcClient<TClient>(_ => { })`
  - See [grpc/grpc-dotnet/issues/2158](https://github.com/grpc/grpc-dotnet/issues/2158)
  - Available in `>=net6.0` only.
- Add `FunctionsGrpcOptions`, which can be used to get a built `CallInvoker` for gRPC communication with the functions host. (#1637)
  - Available in all TFMs.