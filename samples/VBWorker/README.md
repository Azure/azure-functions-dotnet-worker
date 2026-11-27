# Visual Basic Worker

This sample demonstrates how to create a Visual Basic project using the isolated worker model with ASP.NET Core integration.

Visual Basic works with the isolated worker model because the worker discovers functions through the `Function` attribute at build time, the same way it does for C#. The only Visual Basic specific points are:

- `Function` is also a Visual Basic keyword, so the attribute must be written escaped as `<[Function]("HelloHttp")>`.
- The binding attribute goes on the parameter: `<HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")> req As HttpRequest`.
- The entry point is a `Sub Main` in a `Module`.

## Build

```
dotnet build
```

After a build, `bin/Debug/net9.0/functions.metadata` lists the discovered function (`VBWorker.HelloHttp.Run`, an `httpTrigger` binding), which shows the worker SDK picked it up from the Visual Basic assembly.

## Run

Install [Azure Functions Core Tools](https://learn.microsoft.com/azure/azure-functions/functions-run-local), then run `func start` from the `bin/Debug/net9.0` folder (or `func start` in this folder after `dotnet build`).
