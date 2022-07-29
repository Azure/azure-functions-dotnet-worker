// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading;

namespace Microsoft.Azure.Functions.Worker.Invocation
{
    /// <summary>
    /// A representation of an invocation context and associated cancellation source
    /// </summary>
    internal class FunctionInvocationDetails
    {
      internal FunctionContext FunctionContext { get; set; }

      internal CancellationTokenSource? CancellationTokenSource { get; set; }
    }
}