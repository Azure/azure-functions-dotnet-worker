﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Azure.Functions.Worker.Extensions.Rpc
{
    internal static class ConfigurationExtensions
    {
        /// <summary>
        /// Gets the functions host gRPC address from <see cref="IConfiguration"/>.
        /// </summary>
        /// <param name="configuration">The configuration to get the host URI from.</param>
        /// <returns>The URI representing the functions host.</returns>
        /// <exception cref="InvalidOperationException">
        /// If <paramref name="configuration"/> does not contain the required values.
        /// </exception>
        public static Uri GetFunctionsHostGrpcUri(this IConfiguration configuration)
        {
            Uri? grpcUri;
            var functionsUri = configuration["Functions:Worker:HostEndpoint"];
            if (functionsUri is not null)
            {
                if (!Uri.TryCreate(functionsUri, UriKind.Absolute, out grpcUri))
                {
                    throw new InvalidOperationException($"The gRPC channel URI '{functionsUri}' could not be parsed.");
                }
            }
            else
            {
                var uriString = $"http://{configuration["HOST"]}:{configuration["PORT"]}";
                if (!Uri.TryCreate(uriString, UriKind.Absolute, out grpcUri))
                {
                    throw new InvalidOperationException($"The gRPC channel URI '{uriString}' could not be parsed.");
                }
            }

            return grpcUri;
        }

        /// <summary>
        /// Gets the maximum message length for the functions host gRPC channel.
        /// </summary>
        /// <param name="configuration">The configuration to retrieve values from.</param>
        /// <returns>The maximum message length if available. </returns>
        public static int? GetFunctionsHostMaxMessageLength(this IConfiguration configuration)
        {
            return configuration.GetValue<int?>("Functions:Worker:GrpcMaxMessageLength", null)
                ?? configuration.GetValue<int?>("grpcMaxMessageLength", null);
        }
    }
}
