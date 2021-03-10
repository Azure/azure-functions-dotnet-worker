// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Pipeline;

namespace Microsoft.Azure.Functions.Worker.Invocation
{
    internal interface IFunctionExecutor
    {
        Task ExecuteAsync(FunctionContext context);
    }
}
