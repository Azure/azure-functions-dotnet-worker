// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker.Pipeline
{
    internal interface IInvocationPipelineBuilder<Context>
    {
        IInvocationPipelineBuilder<Context> Use(Func<FunctionExecutionDelegate, FunctionExecutionDelegate> middleware);

        FunctionExecutionDelegate Build();
    }
}
