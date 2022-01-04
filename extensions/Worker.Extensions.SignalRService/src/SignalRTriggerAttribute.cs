// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

﻿using System;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Attribute used to mark a function that should be triggered by messages sent from SignalR clients.
    /// </summary>
    /// <remarks>
    /// The attribute target can have following attributes:
    /// <code>
    /// public class InvocationContext
    /// {
    ///     public object[] Arguments { get; set; }
    ///     public string Error { get; set; }
    ///     public string Category { get; set; }
    ///     public string Event { get; set; }
    ///     public string Hub { get; set; }
    ///     public string ConnectionId { get; set; }
    ///     public string UserId { get; set; }
    ///     public IDictionary<string, string> Headers { get; set; }
    ///     public IDictionary<string, string> Query { get; set; }
    ///     public IDictionary<string, string> Claims { get; set; }
    /// }
    /// </code>
    /// </remarks>
    public sealed class SignalRTriggerAttribute : TriggerBindingAttribute
    {
        public SignalRTriggerAttribute(string hubName, string category, string @event) : this(hubName, category, @event, Array.Empty<string>())
        {
        }

        public SignalRTriggerAttribute(string hubName, string category, string @event, params string[] parameterNames)
        {
            HubName = hubName;
            Category = category;
            Event = @event;
            ParameterNames = parameterNames;
        }

        /// <summary>
        /// Gets or sets the app setting name that contains the Azure SignalR connection string.
        /// </summary>
        public string? ConnectionStringSetting { get; set; }

        /// <summary>
        /// The hub of request belongs to.
        /// </summary>
        public string HubName { get; }

        /// <summary>
        /// The event of the request.
        /// </summary>
        public string Event { get; }

        /// <summary>
        /// Two optional value: connections and messages
        /// </summary>
        public string Category { get; }

        /// <summary>
        /// Used for messages category. All the name defined in <see cref="ParameterNames"/> will map to
        /// Arguments in InvocationMessage by order. And the name can be used in parameters of method
        /// directly.
        /// </summary>
        public string[] ParameterNames { get; }
    }
}
