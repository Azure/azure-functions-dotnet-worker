## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Worker.Extensions.Http <version>

- The 'FromBody' converter now utilizes `DeserializeAsync` for deserializing JSON content from the request body, enhancing support for asynchronous deserialization. (#<PR>)
