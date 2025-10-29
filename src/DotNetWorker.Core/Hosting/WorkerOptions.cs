// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text.Json;
using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker.Diagnostics;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// An options class for configuring the worker.
    /// </summary>
    public class WorkerOptions
    {
        /// <summary>
        /// The <see cref="ObjectSerializer"/> to use for all JSON serialization and deserialization. By default,
        /// this is a default <see cref="JsonObjectSerializer"/> with default <see cref="JsonSerializerOptions"/>.
        /// </summary>
        public ObjectSerializer? Serializer { get; set; }

        /// <summary>
        /// Gets the collection of input converters.
        /// </summary>
        public InputConverterCollection InputConverters { get; } = new InputConverterCollection();

        /// <summary>
        /// Gets the optional worker capabilities.
        /// </summary>
        public IDictionary<string, string> Capabilities { get; } = new Dictionary<string, string>()
        {
            // Enable these by default, although they are not strictly required and can be removed
            { WorkerCapabilities.HandlesWorkerTerminateMessage, bool.TrueString },
            { WorkerCapabilities.HandlesInvocationCancelMessage, bool.TrueString },
            { WorkerCapabilities.IncludeEmptyEntriesInMessagePayload, bool.TrueString },
            { WorkerCapabilities.EnableUserCodeException, bool.TrueString }
        };

        /// <summary>
        /// Gets or sets a value indicating whether exceptions thrown by user code should be unwrapped
        /// and surfaced to the Host as their original exception type, instead of being wrapped in an RpcException.
        /// The default value is <see langword="true"/>.
        /// </summary>
        [Obsolete("This is now the default behavior. This property may be unavailable in future releases.", false)]
        public bool EnableUserCodeException
        {
            get => GetBoolCapability(nameof(EnableUserCodeException));
            set => SetBoolCapability(nameof(EnableUserCodeException), value);
        }

        /// <summary>
        /// Gets or sets a value that determines if empty entries should be included in the function trigger message payload.
        /// For example, if a set of entries were sent to a messaging service such as Service Bus or Event Hub and your function
        /// app has a Service bus trigger or Event hub trigger, only the non-empty entries from the payload will be sent to the
        /// function code as trigger data when this setting value is <see langword="false"/>. When it is <see langword="true"/>,
        /// all entries will be sent to the function code as it is. Default value for this setting is <see langword="true"/>.
        /// </summary>
        public bool IncludeEmptyEntriesInMessagePayload
        {
            get => GetBoolCapability(nameof(IncludeEmptyEntriesInMessagePayload));
            set => SetBoolCapability(nameof(IncludeEmptyEntriesInMessagePayload), value);
        }

        /// <summary>
        /// Gets or sets a value that determines the schema to use when generating Activities. Currently internal as there is only
        /// one schema, but stubbing this out for future use.
        /// </summary>
        internal OpenTelemetrySchemaVersion OpenTelemetrySchemaVersion { get; set; } = OpenTelemetrySchemaVersion.V1_17_0;

        private bool GetBoolCapability(string name)
        {
            return Capabilities.TryGetValue(name, out string? value) && bool.TryParse(value, out bool b) && b;
        }

        // For false values, the host does not expect the capability to exist; there are some cases where this
        // will be interpreted as "true" just because the key is there.
        private void SetBoolCapability(string name, bool value)
        {
            if (value)
            {
                Capabilities[name] = bool.TrueString;
            }
            else
            {
                Capabilities.Remove(name);
            }
        }
    }
}
