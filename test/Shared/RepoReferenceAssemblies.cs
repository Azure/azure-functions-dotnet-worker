// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#nullable enable

using System;
using System.IO;
using Microsoft.CodeAnalysis.Testing;

namespace Microsoft.Azure.Functions.Tests.Analyzers
{
    /// <summary>
    /// Shared helpers for wiring the Roslyn analyzer test harness to the repository's
    /// NuGet.Config. Without this, <see cref="ReferenceAssemblies"/> resolves packages
    /// using only the user/machine-level NuGet settings (which default to nuget.org),
    /// causing package downloads from public feeds at test runtime. Routing resolution
    /// through the repo NuGet.Config keeps package restore on the compliant feed.
    /// </summary>
    internal static class RepoReferenceAssemblies
    {
        private const string RepoRootMarker = ".reporoot";
        private const string NuGetConfigFileName = "NuGet.Config";

        private static readonly string s_repoNuGetConfigPath = ResolveRepoNuGetConfigPath();

        /// <summary>
        /// Configures the <see cref="ReferenceAssemblies"/> to resolve NuGet packages
        /// through the repository's <c>NuGet.Config</c> rather than the machine defaults.
        /// </summary>
        public static ReferenceAssemblies WithRepoNuGetConfig(this ReferenceAssemblies referenceAssemblies)
        {
            return referenceAssemblies.WithNuGetConfigFilePath(s_repoNuGetConfigPath);
        }

        private static string ResolveRepoNuGetConfigPath()
        {
            string? current = AppContext.BaseDirectory;
            while (current is not null && !File.Exists(Path.Combine(current, RepoRootMarker)))
            {
                current = Directory.GetParent(current)?.FullName;
            }

            if (current is null)
            {
                throw new InvalidOperationException(
                    $"Could not locate the '{RepoRootMarker}' marker file by walking up from '{AppContext.BaseDirectory}'.");
            }

            return Path.Combine(current, NuGetConfigFileName);
        }
    }
}
