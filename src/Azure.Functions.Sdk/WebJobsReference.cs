// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Azure.Functions.Sdk;

/// <summary>
/// Represents an Azure Functions WebJobs reference.
/// </summary>
/// <param name="name">The name of the package reference.</param>
/// <param name="typeName">The type entry point of the package.</param>
/// <param name="hintPath">The hint path for the package reference.</param>
public partial class WebJobsReference(string name, string typeName, string hintPath)
{
    public string Name { get; } = name ?? string.Empty;

    public string TypeName { get; } = typeName ?? string.Empty;

    public string HintPath { get; } = hintPath ?? string.Empty;
}
