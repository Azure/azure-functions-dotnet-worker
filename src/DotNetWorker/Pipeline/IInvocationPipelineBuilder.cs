// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Worker.Pipeline
{
    public interface IInvocationPipelineBuilder<FunctionExecutionContext>
    {
        IInvocationPipelineBuilder<FunctionExecutionContext> Use(Func<FunctionExecutionDelegate, FunctionExecutionDelegate> middleware);

        FunctionExecutionDelegate Build();
    }
}
