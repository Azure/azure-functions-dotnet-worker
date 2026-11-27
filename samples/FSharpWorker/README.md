# F# Worker

This sample demonstrates how to create an F# project using the isolated worker model with ASP.NET Core integration.

F# works with the isolated worker model because the worker discovers functions through the `[<Function>]` attribute at build time, the same way it does for C#. The only F#-specific points are:

- Functions are members of a class (`HelloHttp`), with dependencies such as `ILogger<HelloHttp>` taken through the primary constructor.
- The binding attribute is placed on the parameter (`[<HttpTrigger(...)>] req: HttpRequest`).
- `Program.fs` must be the last item in `<Compile Include="..." />`, because it holds the `[<EntryPoint>]`.

## Build

```
dotnet build
```

After a build, `bin/Debug/net9.0/functions.metadata` lists the discovered function (`FSharpWorker.HelloHttp.Run`, an `httpTrigger` binding), which shows the worker SDK picked it up from the F# assembly.

## Run

Install [Azure Functions Core Tools](https://learn.microsoft.com/azure/azure-functions/functions-run-local), then run `func start` from the `bin/Debug/net9.0` folder (or `func start` in this folder after `dotnet build`).
