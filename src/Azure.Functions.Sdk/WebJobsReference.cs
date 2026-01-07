// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Azure.Functions.Sdk;

/// <summary>
/// Represents an Azure Functions WebJobs reference.
/// </summary>
/// <param name="name">The name of the WebJobs extension.</param>
/// <param name="version">The assembly-qualified name of the WebJobs extension type.</param>
/// <param name="hintPath">An optional hint path to the WebJobs extension assembly.</param>
public partial class WebJobsReference(string name, string version, string hintPath)
{
    public string Name { get; } = name ?? string.Empty;

    public string Version { get; } = version ?? string.Empty;

    public string HintPath { get; } = hintPath ?? string.Empty;
}
