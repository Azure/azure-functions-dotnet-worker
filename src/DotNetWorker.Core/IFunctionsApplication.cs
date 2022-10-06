// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Core.Diagnostics;

namespace Microsoft.Azure.Functions.Worker
{
    internal interface IFunctionsApplication
    {
        void RaiseHostTelemetryEvent(HostTelemetryEvent telemetry);

        void LoadFunction(FunctionDefinition definition);

        Task InvokeFunctionAsync(FunctionContext context);

        FunctionContext CreateContext(IInvocationFeatures features, CancellationToken token = default);
    }
}
