using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Azure.Functions.Worker.Logging;

namespace Microsoft.Azure.Functions.Worker.Core.Diagnostics
{
    internal static class FunctionActivitySource
    {
        private static readonly ActivitySource _activitySource = new("Microsoft.Azure.Functions.Worker");
        private static readonly string _processId = Process.GetCurrentProcess().Id.ToString();
        private static readonly ConcurrentDictionary<string, string> _categoryCache = new ConcurrentDictionary<string, string>();

        public static Activity? StartInvoke(FunctionContext context)
        {
            var activity = _activitySource.StartActivity("Invoke", ActivityKind.Internal, context.TraceContext.TraceParent);

            if (activity != null)
            {
                activity.AddTag(LogConstants.InvocationIdKey, context.InvocationId);
                activity.AddTag(LogConstants.NameKey, context.FunctionDefinition.Name);
                activity.AddTag(LogConstants.CategoryNameKey, _categoryCache.GetOrAdd(context.FunctionDefinition.Name, functionName => $"Function.{functionName}.User"));
                activity.AddTag(LogConstants.ProcessIdKey, _processId);
            }

            return activity;
        }
    }
}
