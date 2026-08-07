// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker.Testing;

/// <summary>Describes which Azure Functions host behaviors are intentionally outside the in-memory worker test host.</summary>
public sealed class FunctionsTestCapabilities
{
    internal static FunctionsTestCapabilities WorkerOnly { get; } = new();

    private FunctionsTestCapabilities()
    {
    }

    /// <summary>Gets whether built-in HTTP functions are selected by URL route.</summary>
    public bool SupportsBuiltInUrlRouting => false;

    /// <summary>Gets whether Functions host authorization levels and keys are enforced.</summary>
    public bool SupportsAuthorization => false;

    /// <summary>Gets whether trigger listeners are started and scheduled by the Functions host.</summary>
    public bool SupportsTriggerListeners => false;

    /// <summary>Gets whether service-specific message settlement is performed.</summary>
    public bool SupportsMessageSettlement => false;

    /// <summary>Gets whether the Functions host schedules retries.</summary>
    public bool SupportsRetryScheduling => false;

    /// <summary>Gets guidance for tests that require the real Functions host.</summary>
    public string FullHostTestingGuidance { get; }
        = "https://learn.microsoft.com/azure/azure-functions/functions-develop-local";
}
