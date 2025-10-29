// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Azure.Functions.Worker.Diagnostics;

/// <summary>
/// Provides methods for telemetry data collection related to function invocations.
/// </summary>
/// <remarks>This interface defines methods to retrieve telemetry attributes and manage the activity lifecycle for
/// function invocations, enabling detailed monitoring and diagnostics.</remarks>
internal interface IFunctionTelemetryProvider
{
    /// <summary>
    /// Returns the attributes to be applied to the Activity/Scope for this invocation.
    /// </summary>
    IEnumerable<KeyValuePair<string, object>> GetTelemetryAttributes(FunctionContext ctx);

    /// <summary>
    /// Starts the Activity for this invocation.
    /// </summary>
    Activity? StartActivityForInvocation(FunctionContext ctx);
}
