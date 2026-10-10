// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.Azure.Functions.Worker.Extensions.AzureCosmosDb.Mongo.Auth
{
    internal sealed class ConnectionStringParser
    {
        private readonly string _originalConnectionString;

        public ConnectionStringParser(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentNullException(nameof(connectionString));
            }

            _originalConnectionString = connectionString;
            ParseConnectionString();
        }

        public string OriginalConnectionString => _originalConnectionString;

        public string? ClientId { get; private set; }

        public string? Host { get; private set; }

        public string? HostsWithPorts { get; private set; }

        public string? Scheme { get; private set; }

        public string? Database { get; private set; }

        public Dictionary<string, string> QueryParameters { get; private set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public bool IsEntraIdConnectionString { get; private set; }

        public Exception? ParsingException { get; private set; }

        public bool ParsedSuccessfully => ParsingException == null;

        private void ParseConnectionString()
        {
            try
            {
                var connectionString = _originalConnectionString;

                var schemeMatch = Regex.Match(connectionString, @"^(mongodb(?:\+srv)?):\/\/");
                if (!schemeMatch.Success)
                {
                    throw new FormatException("Invalid MongoDB connection string: missing or invalid scheme.");
                }
                Scheme = schemeMatch.Groups[1].Value;
                connectionString = connectionString.Substring(schemeMatch.Length);

                var credMatch = Regex.Match(connectionString, @"^([^:@]+)(?::([^@]*))?@");
                if (credMatch.Success)
                {
                    var username = Uri.UnescapeDataString(credMatch.Groups[1].Value);

                    if (IsGuidFormat(username))
                    {
                        ClientId = username;
                    }

                    connectionString = connectionString.Substring(credMatch.Length);
                }

                var pathQuerySplit = connectionString.Split(new[] { '?' }, 2);
                var pathPart = pathQuerySplit[0];
                var queryPart = pathQuerySplit.Length > 1 ? pathQuerySplit[1] : null;

                var hostDbSplit = pathPart.Split(new[] { '/' }, 2);
                HostsWithPorts = hostDbSplit[0];

                Host = HostsWithPorts.Split(',')[0].Split(':')[0];

                if (hostDbSplit.Length > 1 && !string.IsNullOrEmpty(hostDbSplit[1]))
                {
                    Database = hostDbSplit[1];
                }

                if (!string.IsNullOrEmpty(queryPart))
                {
                    foreach (var keyValue in queryPart!.Split('&').Select(p => p.Split(new[] { '=' }, 2)))
                    {
                        if (keyValue.Length == 2)
                        {
                            QueryParameters[Uri.UnescapeDataString(keyValue[0])] = Uri.UnescapeDataString(keyValue[1]);
                        }
                    }
                }

                if (QueryParameters.TryGetValue("authMechanism", out var authMechanism) &&
                    string.Equals(authMechanism, "MONGODB-OIDC", StringComparison.OrdinalIgnoreCase))
                {
                    IsEntraIdConnectionString = true;
                }
            }
            catch (Exception ex)
            {
                IsEntraIdConnectionString = false;
                ParsingException = ex;
            }
        }

        public string PrepareForEntraIdAuth()
        {
            if (!ParsedSuccessfully)
            {
                throw ParsingException!;
            }

            var sb = new StringBuilder();
            sb.Append(Scheme);
            sb.Append("://");

            sb.Append(HostsWithPorts);

            if (!string.IsNullOrEmpty(Database))
            {
                sb.Append('/');
                sb.Append(Database);
            }

            var filteredParams = QueryParameters
                .Where(kv => !IsAuthRelatedParameter(kv.Key))
                .ToList();

            if (!filteredParams.Any(kv => kv.Key.Equals("tls", StringComparison.OrdinalIgnoreCase) ||
                                          kv.Key.Equals("ssl", StringComparison.OrdinalIgnoreCase)))
            {
                filteredParams.Add(new KeyValuePair<string, string>("tls", "true"));
            }

            if (filteredParams.Any())
            {
                sb.Append('?');
                sb.Append(string.Join("&", filteredParams.Select(kv =>
                    $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}")));
            }

            return sb.ToString();
        }

        private static bool IsAuthRelatedParameter(string paramName)
        {
            var authParams = new[]
            {
                "authMechanism",
                "authSource",
                "authMechanismProperties"
            };
            return authParams.Any(p => p.Equals(paramName, StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsGuidFormat(string value)
        {
            return Guid.TryParse(value, out _);
        }
    }
}