// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading;

namespace Microsoft.Azure.Functions.Worker.Pipeline
{
    internal interface IFunctionContextFactory
    {
        FunctionContext Create(IInvocationFeatures features, CancellationToken token);
    }
}
