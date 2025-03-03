// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

﻿using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

namespace Microsoft.Azure.Functions.Worker
{
    public sealed class KafkaOutputAttribute : OutputBindingAttribute
    {
        /// <summary>
        /// Initialize a new instance of the <see cref="KafkaOutputAttribute"/>
        /// </summary>
        /// <param name="brokerList">Broker list</param>
        /// <param name="topic">Topic name</param>
        public KafkaOutputAttribute(string brokerList, string topic)
        {
            BrokerList = brokerList;
            Topic = topic;
        }

        /// <summary>
        /// The topic name hub.
        /// </summary>
        public string Topic { get; private set; }

        /// <summary>
        /// Gets or sets the Broker List.
        /// </summary>
        public string BrokerList { get; set; }

        /// <summary>
        /// Gets or sets the Avro schema.
        /// Should be used only if a generic record should be generated
        /// </summary>
        public string AvroSchema { get; set; }

        /// <summary>
        /// Gets or sets the Avro schema of message key.
        /// Should be used only if a generic record should be generated.
        /// </summary>
        public string KeyAvroSchema { get; set; }

        /// <summary>
        /// Specifies the data type of the message key.
        /// This data type will be used to serialize the key before sending it to the Kafka topic.
        /// If KeyAvroSchema is set, this value is ignored and the key will be serialized using Avro.
        /// The default type is System.String.
        /// </summary>
        public KafkaMessageKeyType KeyDataType { get; set; } = KafkaMessageKeyType.String;

        /// <summary>
        /// Gets or sets the maximum transmit message size in bytes. Default: 1MB
        /// </summary>
        public int MaxMessageBytes { get; set; } = 1_000_000;

        /// <summary>
        /// Maximum number of messages batched in one MessageSet. default: 10000
        /// </summary>
        public int BatchSize { get; set; } = 10_000;

        /// <summary>
        /// When set to `true`, the producer will ensure that messages are successfully produced exactly once and in the original produce order. default: false
        /// </summary>
        public bool EnableIdempotence { get; set; } = false;

        /// <summary>
        /// Local message timeout. This value is only enforced locally and limits the time a produced message waits for successful delivery. A time of 0 is infinite. This is the maximum time used to deliver a message (including retries). Delivery error occurs when either the retry count or the message timeout are exceeded. default: 300000
        /// </summary>
        public int MessageTimeoutMs { get; set; } = 300_000;

        /// <summary>
        /// The ack timeout of the producer request in milliseconds. default: 5000
        /// </summary>
        public int RequestTimeoutMs { get; set; } = 5_000;

        /// <summary>
        /// How many times to retry sending a failing Message. **Note:** default: 2 
        /// </summary>
        /// <remarks>Retrying may cause reordering unless <c>EnableIdempotence</c> is set to <c>true</c>.</remarks>
        public int MaxRetries { get; set; } = int.MaxValue;

        /// <summary>
        /// SASL mechanism to use for authentication. 
        /// Allowed values: Gssapi, Plain, ScramSha256, ScramSha512, OAuthBearer
        /// Default: Plain
        /// 
        /// sasl.mechanism in librdkafka
        /// </summary>
        public BrokerAuthenticationMode AuthenticationMode { get; set; } = BrokerAuthenticationMode.NotSet;

        /// <summary>
        /// SASL username for use with the PLAIN and SASL-SCRAM-.. mechanisms
        /// Default: ""
        /// 
        /// 'sasl.username' in librdkafka
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// SASL password for use with the PLAIN and SASL-SCRAM-.. mechanism
        /// Default: ""
        /// 
        /// sasl.password in librdkafka
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets the security protocol used to communicate with brokers
        /// Default is plain text
        /// 
        /// security.protocol in librdkafka
        /// </summary>
        public BrokerProtocol Protocol { get; set; } = BrokerProtocol.NotSet;

        /// <summary>
        /// Path to client's private key (PEM) used for authentication.
        /// Default: ""
        /// ssl.key.location in librdkafka
        /// </summary>
        public string SslKeyLocation { get; set; }

        /// <summary>
        /// Path to CA certificate file for verifying the broker's certificate.
        /// ssl.ca.location in librdkafka
        /// </summary>
        public string SslCaLocation { get; set; }

        /// <summary>
        /// Path to client's certificate.
        /// ssl.certificate.location in librdkafka
        /// </summary>
        public string SslCertificateLocation { get; set; }

        /// <summary>
        /// Password for client's certificate.
        /// ssl.key.password in librdkafka
        /// </summary>
        public string SslKeyPassword { get; set; }

        /// <summary>
        /// Client certificate in PEM format.
        /// ssl.certificate.pem in librdkafka
        /// </summary>
        public string SslCertificatePEM { get; set; }

        /// <summary>
        /// Client Private Key in PEM format.
        /// ssl.key.pem in librdkafka
        /// </summary>
        public string SslKeyPEM { get; set; }

        /// <summary>
        /// CA certificate for verifying the broker's certificate in PEM format
        /// ssl.ca.pem in librdkafka
        /// </summary>
        public string SslCaPEM { get; set; }

        /// <summary>
        /// Client certificate and key in PEM format.
        /// Additional Configuration for extension as KeyVault supports uploading certificate only with private key. 
        /// </summary>
        public string SslCertificateandKeyPEM { get; set; }

        /// <summary>
        /// Linger.MS property provides the time between batches of messages
        /// being sent to cluster. Larger value allows more batching results in high throughput.
        /// </summary>
        public int LingerMs { get; set; } = 5;

        /// <summary>
        /// URL for the Avro Schema Registry
        /// </summary>
        public string SchemaRegistryUrl { get; set; }

        /// <summary>
        /// Username for the Avro Schema Registry
        /// </summary>
        public string SchemaRegistryUsername { get; set; }

        /// <summary>
        /// Password for the Avro Schema Registry
        /// </summary>
        public string SchemaRegistryPassword { get; set; }

        /// <summary>
        /// OAuth Bearer method.
        /// Either 'default' or 'oidc'
        /// sasl.oauthbearer in librdkafka
        /// </summary>
        public OAuthBearerMethod OAuthBearerMethod { get; set; }

        /// <summary>
        /// OAuth Bearer Client Id
        /// Specify only when OAuthBearerMethod is 'oidc'
        /// sasl.oauthbearer.client.id in librdkafka
        /// </summary>
        public string OAuthBearerClientId { get; set; }

        /// <summary>
        /// OAuth Bearer Client Secret
        /// Specify only when OAuthBearerMethod is 'oidc'
        /// sasl.oauthbearer.client.secret in librdkafka
        /// </summary>
        public string OAuthBearerClientSecret { get; set; }

        /// <summary>
        /// OAuth Bearer scope.
        /// Client use this to specify the scope of the access request to the broker. 
        /// Specify only when OAuthBearerMethod is 'oidc'
        /// sasl.oauthbearer.extensions in librdkafka
        /// </summary>
        public string OAuthBearerScope { get; set; }

        /// <summary>
        /// OAuth Bearer token endpoint url.
        /// Specify only when OAuthBearerMethod is 'oidc'
        /// sasl.oauthbearer.token.endpoint.url in librdkafka
        /// </summary>
        public string OAuthBearerTokenEndpointUrl { get; set; }

        /// <summary>
        /// OAuth Bearer extensions.
        /// Allow additional information to be provided to the broker.
        /// Comma-separated list of key=value pairs. E.g., "supportFeatureX=true,organizationId=sales-emea"
        /// sasl.oauthbearer.extensions in librdkafka
        /// </summary>
        public string OAuthBearerExtensions { get; set; }
    }
}
