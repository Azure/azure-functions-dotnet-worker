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
        /// <exception cref="UriFormatException">If <paramref name="configuration"/> contain a value for 'functions-uri', but is not a valid URL.</exception>
        /// <exception cref="InvalidOperationException">
        /// If <paramref name="configuration"/> does not contain the required values.
        /// </exception>
        public static Uri GetFunctionsHostGrpcUri(this IConfiguration configuration)
        {
            Uri? grpcUri;
            var functionsUri = configuration["functions-uri"];
            if (functionsUri is not null)
            {
                if (!Uri.TryCreate(functionsUri, UriKind.Absolute, out grpcUri))
                {
                    throw new UriFormatException($"'{functionsUri}' is not a valid value for 'functions-uri'. Value should be a valid URL.");
                }
            }
            else
            {
                string uriString = $"http://{configuration["HOST"]}:{configuration["PORT"]}";
                if (!Uri.TryCreate(uriString, UriKind.Absolute, out grpcUri))
                {
                    throw new InvalidOperationException($"The gRPC channel URI '{uriString}' could not be parsed.");
                }
            }

            return grpcUri;
        }
    }
}
