### Release notes
<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->
- API to enable simple registration of middleware
- Add support for batched trigger events (#205)
  - The following services allow trigger events to be batched:
    - Event Hubs (batched by default)
    - Service Bus (set `IsBatched = true` in trigger attribute)
    - Kafka (set `IsBatched = true` in trigger attribute)
  - To read batched event data in function code:
    - Use array (`[]`), `IList`, `ICollection`, or `IEnumerable` if event data is `string`, `byte[]`, or `ReadOnlyMemory<byte>` (example: `string[]`).
      - Note: `ReadOnlyMemory<byte>` is the more performant option to read binary data, especially for large payloads.
    - Use a class that implements `IEnumerable` or `IEnumerable<T>` for serializable event data (example: `List<MyData>`).
- Fail function execution if the requested parameter cannot be converted to the specified type (#216)
- Fix issue with referencing extensions that have native dependencies:
  - To address this, a default dependency on .NET Core 3.1 SDK was added. Note: This dependency can be overridden by setting a build property `<_FunctionsExtensionTargetFramework>netstandard2.0<_FunctionsExtensionTargetFramework/>` if only using bindings without native dependencies.
