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
      /// <summary>
      /// Encapsulates the information about a function execution
      /// </summary>
      internal FunctionContext FunctionContext { get; set; }

      /// <summary>
      /// Source used to create and cancel a cancellation token; can be null.
      /// </summary>
      internal CancellationTokenSource? CancellationTokenSource { get; set; }
    }
}