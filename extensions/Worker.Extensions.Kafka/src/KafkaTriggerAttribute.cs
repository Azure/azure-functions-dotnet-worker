// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

﻿using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

namespace Microsoft.Azure.Functions.Worker
{
    [BindingCapabilities(KnownBindingCapabilities.FunctionLevelRetry)]
    public sealed class KafkaTriggerAttribute : TriggerBindingAttribute, ISupportCardinality
    {
        private bool _isBatched = false;

        /// <summary>
        /// Initialize a new instance of the <see cref="KafkaTriggerAttribute"/>
        /// </summary>
        /// <param name="brokerList">Name of broker list</param>
        /// <param name="topic">Name of topic</param>
        public KafkaTriggerAttribute(string brokerList, string topic)
        {
            BrokerList = brokerList;
            Topic = topic;
        }

        /// <summary>
        /// Gets or sets the topic
        /// </summary>
        public string Topic { get; private set; }

        /// <summary>
        /// Gets or sets the broker list
        /// </summary>
        public string BrokerList { get; private set; }

        /// <summary>
        /// Gets or sets the consumer group
        /// </summary>
        public string ConsumerGroup { get; set; }

        /// <summary>
        /// Gets or sets the Avro schema of message value.
        /// Should be used only if a generic record should be generated
        /// </summary>
        public string AvroSchema { get; set; }

        /// <summary>
        /// Gets or sets the Avro schema of message key.
        /// Should be used only if a generic record should be generated
        /// </summary>
        public string KeyAvroSchema { get; set; }

        /// <summary>
        /// Specifies the data type of the message key.
        /// This data type will be used to serialize the key before sending it to the Kafka topic.
        /// If KeyAvroSchema is set, this value is ignored and the key will be serialized using Avro.
        /// The default type is System.String.
        /// </summary>
        public KafkaMessageKeyDataType KeyDataType { get; set; } = KafkaMessageKeyDataType.String;

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
        /// Maximum number of unprocessed messages a worker is expected to have at an instance.
        /// When target-based scaling is not disabled, this is used to divide the 
        /// total unprocessed event count to determine the number of worker instances, 
        /// which will then be rounded up to a worker instance count that creates a balanced partition distribution.
        /// Default: 1000
        /// </summary>
        public long LagThreshold { get; set; } = 1000;

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
        
        /// <summary>
        /// Gets or sets the configuration to enable batch processing of events. Default value is "false".
        /// </summary>
        [DefaultValue(false)]
        public bool IsBatched
        {
            get => _isBatched;
            set => _isBatched = value;
        }

        Cardinality ISupportCardinality.Cardinality
        {
            get
            {
                if (_isBatched)
                {
                    return Cardinality.Many;
                }
                else
                {
                    return Cardinality.One;
                }
            }
            set
            {
                if (value.Equals(Cardinality.Many))
                {
                    _isBatched = true;
                }
                else
                {
                    _isBatched = false;
                }
            }
        }
    }
}
