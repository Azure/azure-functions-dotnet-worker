## Release notes
<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### 3.6.0
- Confluent library upgrade from 1.6.3 to 1.9.0 for fixing duplicate record processing issue (https://github.com/Azure/azure-functions-kafka-extension/pull/357)
- Fixed the bug for accessing the certificate from relative path instead of absolute path (https://github.com/Azure/azure-functions-kafka-extension/pull/386)

### 3.5.0
- Added the improvement for the producer logging to app insights & filesystem. (https://github.com/Azure/azure-functions-kafka-extension/pull/377)
- Added the support for the retry feature (https://github.com/Azure/azure-functions-kafka-extension/pull/363/ https://github.com/Azure/azure-functions-kafka-extension/issues/122)
- Added the support for keys for triggers & output bindings (https://github.com/Azure/azure-functions-kafka-extension/pull/328/ https://github.com/Azure/azure-functions-kafka-extension/issues/326)
- Added the support for auto.offset.reset config (https://github.com/Azure/azure-functions-kafka-extension/pull/364/ https://github.com/Azure/azure-functions-kafka-extension/issues/249)