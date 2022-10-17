# .NET Framework Worker

This sample demonstrates how to create a .NET 7 project using the Isolated model.

## Function Samples

### HTTPFunction.cs

Demonstrates basic HTTP trigger function.

### EventHubCancellationToken.cs

Demonstrates how to use the Cancellation Token parameter binding in the context of an
EventHub trigger sample where we are processing multiple messages.

- `ThrowOnCancellation`
  - shows how to throw when a cancellation token is received
  - the status of the function will be "Cancelled"
- `HandleCancellationCleanup`
  - shows how to take precautionary/clean up actions if a cancellation token is received
  - the status of the function will be "Successful"

Cancellation tokens are signalled by the platform, the use cases supported today are:

1. Sudden host shutdown or host restart
2. Function timeout (where function execution has exceeded the timeout limit)
   1. You can try this out by setting function timeout to 5 seconds in
      the host.json file: `"functionTimeout": "00:00:05"`
