// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#nullable disable

using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Worker.Invocation
{
    internal interface IFunctionInvoker
    {
        object CreateInstance(FunctionContext context);

        Task<object> InvokeAsync(object instance, object[] arguments);
    }
}
