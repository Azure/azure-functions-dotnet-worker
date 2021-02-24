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
        public string? AvroSchema { get; set; }

        /// <summary>
        /// Gets or sets the Maximum transmit message size. Default: 1MB
        /// </summary>
        public int? MaxMessageBytes { get; set; }

        /// <summary>
        /// Maximum number of messages batched in one MessageSet. default: 10000
        /// </summary>
        public int? BatchSize { get; set; }

        /// <summary>
        /// When set to `true`, the producer will ensure that messages are successfully produced exactly once and in the original produce order. default: false
        /// </summary>
        public bool? EnableIdempotence { get; set; }

        /// <summary>
        /// Local message timeout. This value is only enforced locally and limits the time a produced message waits for successful delivery. A time of 0 is infinite. This is the maximum time used to deliver a message (including retries). Delivery error occurs when either the retry count or the message timeout are exceeded. default: 300000
        /// </summary>
        public int? MessageTimeoutMs { get; set; }

        /// <summary>
        /// The ack timeout of the producer request in milliseconds. default: 5000
        /// </summary>
        public int? RequestTimeoutMs { get; set; }

        /// <summary>
        /// How many times to retry sending a failing Message. **Note:** default: 2 
        /// </summary>
        /// <remarks>Retrying may cause reordering unless <c>EnableIdempotence</c> is set to <c>true</c>.</remarks>
        public int? MaxRetries { get; set; }

        /// <summary>
        /// SASL mechanism to use for authentication. 
        /// Allowed values: Gssapi, Plain, ScramSha256, ScramSha512
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
        public string? Username { get; set; }

        /// <summary>
        /// SASL password for use with the PLAIN and SASL-SCRAM-.. mechanism
        /// Default: ""
        /// 
        /// sasl.password in librdkafka
        /// </summary>
        public string? Password { get; set; }

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
        public string? SslKeyLocation { get; set; }

        /// <summary>
        /// Path to CA certificate file for verifying the broker's certificate.
        /// ssl.ca.location in librdkafka
        /// </summary>
        public string? SslCaLocation { get; set; }

        /// <summary>
        /// Path to client's certificate.
        /// ssl.certificate.location in librdkafka
        /// </summary>
        public string? SslCertificateLocation { get; set; }

        /// <summary>
        /// Password for client's certificate.
        /// ssl.key.password in librdkafka
        /// </summary>
        public string? SslKeyPassword { get; set; }
    }
}
