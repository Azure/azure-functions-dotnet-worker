// Copyright (c) .NET Foundation. All rights reserved.
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
            string uriString = $"http://{configuration["HOST"]}:{configuration["PORT"]}";
            if (!Uri.TryCreate(uriString, UriKind.Absolute, out Uri? grpcUri))
            {
                throw new InvalidOperationException($"The gRPC channel URI '{uriString}' could not be parsed.");
            }

            return grpcUri;
        }
    }
}
