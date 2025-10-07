// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Text.RegularExpressions;

namespace Azure.Functions.Sdk;

/// <summary>
/// Class to help with scanning functions related assemblies.
/// </summary>
public sealed partial class FunctionsAssemblyScanner
{
    private static readonly Regex ExcludedPackagesRegex = new(
        @"^(System|Azure\.Core|Azure\.Identity|Microsoft\.Bcl|Microsoft\.Extensions|Microsoft\.Identity|Microsoft\.NETCore|Microsoft\.NETStandard|Microsoft\.Win32|Grpc)(\..*|$)",
        RegexOptions.Compiled);

    /// <summary>
    /// Checks if the given package name should be scanned or not.
    /// </summary>
    /// <param name="name">The name of the package.</param>
    /// <returns><c>true</c> if package should be scanned, <c>false</c> otherwise.</returns>
    public static bool ShouldScanPackage(string name)
    {
        return !string.IsNullOrEmpty(name) && !ExcludedPackagesRegex.IsMatch(name);
    }
}
