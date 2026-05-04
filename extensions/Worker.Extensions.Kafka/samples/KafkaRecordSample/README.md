# KafkaRecord Isolated Worker Sample

Demonstrates `KafkaRecord` deferred-binding in a .NET isolated worker function.

## Prerequisites

- .NET 8 SDK
- Azure Functions Core Tools v4
- Local Kafka broker on `localhost:9092` with topic `test-topic`

## Run

```bash
dotnet build
func start
```

## Test

```bash
# Produce a message
curl "http://localhost:7071/api/produce?message=hello-kafkarecord"

# Check received records
curl "http://localhost:7071/api/status?message=hello-kafkarecord"
```

## What this validates

- `KafkaRecord` type binds via Protobuf deferred binding (`AzureKafkaRecord`)
- Record metadata (topic, partition, offset, key, value, timestamp, headers) round-trips correctly
- `LeaderEpoch` is NOT present on `KafkaRecord` (removed per issue #639)
