using System;
using System.Collections.Generic;
using System.Security.Authentication;
using Azure.Core;
using Azure.Identity;

namespace Microsoft.Azure.Functions.Worker.ApplicationInsights.Initializers
{
    internal class TokenCredentialOptions
    {
        private const string AuthToken = "AAD";
        private const string AuthAuthorizationKey = "Authorization";
        private const string AuthClientIdKey = "ClientId";

        /// <summary>
        /// The client ID of a user-assigned identity.
        /// </summary>
        /// <remarks>
        /// This must be specified if you're using user-assigned managed identity.
        /// </remarks>
        public string? ClientId { get; set; }

        /// <summary>
        /// Create an <see cref="TokenCredential"/> from an authentication string.
        /// </summary>
        /// <returns>New <see cref="ManagedIdentityCredential"/> from the authentication string.</returns>
        internal TokenCredential CreateTokenCredential()
        {
            return new ManagedIdentityCredential(ClientId);
        }

        /// <summary>
        /// Create an <see cref="TokenCredentialOptions"/> from an authentication string.
        /// </summary>
        /// <returns>New <see cref="TokenCredentialOptions"/> from the authentication string.</returns>
        public static TokenCredentialOptions ParseAuthenticationString(string applicationInsightsAuthenticationString)
        {
            if (string.IsNullOrWhiteSpace(applicationInsightsAuthenticationString))
            {
                throw new ArgumentNullException(nameof(applicationInsightsAuthenticationString), "Authentication string cannot be null or empty.");
            }

            var tokenCredentialOptions = new TokenCredentialOptions();
            bool isValidConfiguration = false;

            foreach ((int, int) split in Tokenize(applicationInsightsAuthenticationString))
            {
                (int start, int length) = split;

                var authenticationStringToken = applicationInsightsAuthenticationString
                    .AsSpan(start, length)
                    .Trim();

                // Ignore (allow) empty tokens.
                if (authenticationStringToken.IsEmpty)
                {
                    continue;
                }

                // Find key-value separator.
                int indexOfEquals = authenticationStringToken.IndexOf('=');
                if (indexOfEquals < 0)
                {
                    continue;
                }

                // Extract key
                var key = authenticationStringToken[..indexOfEquals].TrimEnd();
                if (key.IsEmpty)
                {
                    // Key is blank
                    continue;
                }

                // check if the span matches the string "df":
                if (key.CompareTo( AuthAuthorizationKey.AsSpan(), StringComparison.OrdinalIgnoreCase) == 0)
                {
                    if (authenticationStringToken[(indexOfEquals + 1)..].CompareTo(AuthToken.AsSpan(), StringComparison.OrdinalIgnoreCase) != 0)
                    {
                        throw new InvalidCredentialException("Credential supplied is not valid for the authorization mechanism being used in ApplicationInsights.");
                    }
                    isValidConfiguration = true;
                    continue;
                }

                if (key.CompareTo(AuthClientIdKey.AsSpan(), StringComparison.OrdinalIgnoreCase) == 0)
                {
                    var clientId = authenticationStringToken[(indexOfEquals + 1)..].Trim().ToString();
                    if (!Guid.TryParse(clientId, out Guid _))
                    {
                        throw new FormatException($"The Application Insights AuthenticationString {AuthClientIdKey} is not a valid GUID.");
                    }
                    tokenCredentialOptions.ClientId = clientId;
                }
            }

            // Throw if the Authorization key is not present in the authentication string
            if (!isValidConfiguration)
            {
                throw new InvalidCredentialException("Authorization key is missing in the authentication string for ApplicationInsights.");
            }

            return tokenCredentialOptions;
        }

        private static IEnumerable<(int start, int length)> Tokenize(string value, char separator = ';')
        {
            for (int start = 0, end; start < value.Length; start = end + 1)
            {
                end = value.IndexOf(separator, start);
                if (end < 0)
                {
                    end = value.Length;
                }

                yield return (start, end - start);
            }
        }
    }
}
