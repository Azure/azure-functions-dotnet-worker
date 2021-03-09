// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#nullable disable

using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Worker.Invocation
{
    internal interface IMethodInvoker<TInstance, TReturn>
    {
        // The cancellation token, if any, is provided along with the other arguments.
        Task<TReturn> InvokeAsync(TInstance instance, object[] arguments);
    }
}
