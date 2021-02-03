// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker.Context;

namespace Microsoft.Azure.Functions.Worker.Pipeline
{
    internal interface IFunctionExecutionContextFactory
    {
        FunctionExecutionContext Create(FunctionInvocation invocation, FunctionDefinition definition);
    }
}
