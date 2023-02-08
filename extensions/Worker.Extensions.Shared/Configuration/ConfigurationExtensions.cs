// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Extensions.Configuration;

namespace Microsoft.Azure.Functions.Worker.Extensions
{
    internal static class ConfigurationExtensions
    {
        private const string WebJobsConfigurationSectionName = "AzureWebJobs";
        private const string ConnectionStringsConfigurationSectionName = "ConnectionStrings";

        /// <summary>
        /// Gets the configuration section for a given connection name.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="connectionName">The connection string key.</param>
        internal static IConfigurationSection GetWebJobsConnectionStringSection(this IConfiguration configuration, string connectionName)
        {
            // first try prefixing
            string prefixedConnectionStringName = GetPrefixedConnectionStringName(connectionName);
            IConfigurationSection section = configuration.GetConnectionStringOrSetting(prefixedConnectionStringName);

            if (!section.Exists())
            {
                // next try a direct un-prefixed lookup
                section = configuration.GetConnectionStringOrSetting(connectionName);
            }

            return section;
        }

        /// <summary>
        /// Creates a WebJobs specific prefixed string using a given connection name.
        /// </summary>
        /// <param name="connectionName">The connection string key.</param>
        private static string GetPrefixedConnectionStringName(string connectionName)
        {
            return WebJobsConfigurationSectionName + connectionName;
        }

        /// <summary>
        /// Looks for a connection string by first checking the ConfigurationStrings section, and then the root.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="connectionName">The connection string key.</param>
        private static IConfigurationSection GetConnectionStringOrSetting(this IConfiguration configuration, string connectionName)
        {
            if (configuration.GetSection(ConnectionStringsConfigurationSectionName).Exists())
            {
                IConfigurationSection onConnectionStrings = configuration.GetSection(ConnectionStringsConfigurationSectionName).GetSection(connectionName);
                if (onConnectionStrings.Exists())
                {
                    return onConnectionStrings;
                }
            }

            return configuration.GetSection(connectionName);
        }
    }
}