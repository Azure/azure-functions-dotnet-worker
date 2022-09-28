// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Azure.Functions.Worker.Core.Diagnostics
{
    /// Note: This class will eventually move in to the core worker assembly. Including it in the
    ///       ApplicationInsights package so we can utilize it during preview.
    internal static class FunctionActivitySource
    {
        private const string InvocationIdKey = "InvocationId";
        private const string NameKey = "Name";
        private const string ProcessIdKey = "ProcessId";

        private static readonly ActivitySource _activitySource = new("Microsoft.Azure.Functions.Worker");
        private static readonly string _processId = Process.GetCurrentProcess().Id.ToString();

        public static Activity? StartInvoke(FunctionContext context)
        {
            var activity = _activitySource.StartActivity("Invoke", ActivityKind.Internal, context.TraceContext.TraceParent,
                tags: GetTags(context));

            return activity;
        }

        private static IEnumerable<KeyValuePair<string, object?>> GetTags(FunctionContext context)
        {
            yield return new KeyValuePair<string, object?>(InvocationIdKey, context.InvocationId);
            yield return new KeyValuePair<string, object?>(NameKey, context.FunctionDefinition.Name);
            yield return new KeyValuePair<string, object?>(ProcessIdKey, _processId);
        }
    }
}
