# Middleware Binding

This sample demonstrates how to read an input binding from inside a custom middleware by calling `FunctionContext.BindInputAsync<T>`.

The middleware in `ServiceBusMessageMiddleware.cs` looks up the Service Bus trigger binding of the function that is about to run, binds it to a `ServiceBusReceivedMessage` and logs the message ID before calling the next step in the pipeline. `Program.cs` registers it with `UseWhen`, so it only runs for functions that have a Service Bus trigger.

## Running the sample

Set the `sbconnection` value in `local.settings.json` to a Service Bus connection string and make sure a queue named `test` exists, then start the app with `func start`.

For the general middleware setup see the [Custom Middleware](../CustomMiddleware) sample.
