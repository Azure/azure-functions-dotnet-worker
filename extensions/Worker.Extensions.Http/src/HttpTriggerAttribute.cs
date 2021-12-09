// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

﻿using System;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Attribute used to mark a function that should be invoked over HTTP.
    /// </summary>
    public sealed class HttpTriggerAttribute : TriggerBindingAttribute
    {
        /// <summary>
        /// Creates an instance of the <see cref="HttpTriggerAttribute"/>.
        /// </summary>
        public HttpTriggerAttribute()
        {
            AuthLevel = AuthorizationLevel.Function;
        }

        /// <summary>
        /// Creates an instance of the <see cref="HttpTriggerAttribute"/>, specifying the HTTP methods
        /// the function supports.
        /// </summary>
        /// <param name="methods">The HTTP methods supported by the function.</param>
        public HttpTriggerAttribute(params string[] methods)
        {
            Methods = methods;
        }

        /// <summary>
        /// Creates an instance of the <see cref="HttpTriggerAttribute"/>, specifying the
        /// required <see cref="AuthorizationLevel"/>.
        /// </summary>
        /// <param name="authLevel">The <see cref="AuthorizationLevel"/> to apply.</param>
        public HttpTriggerAttribute(AuthorizationLevel authLevel)
        {
            AuthLevel = authLevel;
        }

        /// <summary>
        /// Creates an instance of the <see cref="HttpTriggerAttribute"/>, specifying the
        /// required <see cref="AuthorizationLevel"/> and supported HTTP methods.
        /// </summary>
        /// <param name="authLevel">The <see cref="AuthorizationLevel"/> to apply.</param>
        /// <param name="methods">The http methods to allow.</param>
        public HttpTriggerAttribute(AuthorizationLevel authLevel, params string[] methods)
        {
            AuthLevel = authLevel;
            Methods = methods;
        }

        /// <summary>
        /// Gets or sets the route template for the function. Can include
        /// route parameters using ASP.NET Core supported syntax. If not specified,
        /// will default to the function name.
        /// </summary>
        public string? Route { get; set; }

        /// <summary>
        /// Gets the HTTP methods that are supported for the function.
        /// </summary>
        public string[]? Methods { get; private set; }

        /// <summary>
        /// Gets the authorization level for the function.
        /// </summary>
        public AuthorizationLevel AuthLevel { get; private set; }
    }
}
