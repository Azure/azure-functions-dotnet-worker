## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Worker.Extensions.Kafka 4.3.0

- **Breaking**: Remove `LeaderEpoch` from `KafkaRecord` — it is consumer fetch metadata, not stored record data (azure-functions-kafka-extension#639)
- Update `Microsoft.Azure.WebJobs.Extensions.Kafka` dependency from 4.3.1 to 4.3.2

### Microsoft.Azure.Functions.Worker.Extensions.Kafka 4.2.0

- Add `KafkaRecord` type for raw Apache Kafka record binding with full metadata access (topic, partition, offset, key/value as raw bytes, headers, timestamp, leader epoch) via Protobuf deserialization (#3356)
- Update `Microsoft.Azure.WebJobs.Extensions.Kafka` dependency from 4.1.4 to 4.3.1 (#3356)
- Add `KafkaRecordConverter` with `SupportsDeferredBinding` following EventHubs/ServiceBus pattern (#3356)
