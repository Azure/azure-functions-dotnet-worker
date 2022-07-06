using System.Collections.Concurrent;
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
        private static readonly ConcurrentDictionary<string, string> _categoryCache = new ConcurrentDictionary<string, string>();

        public static Activity? StartInvoke(FunctionContext context)
        {
            var activity = _activitySource.StartActivity("Invoke", ActivityKind.Internal, context.TraceContext.TraceParent);

            if (activity != null)
            {
                activity.AddTag(InvocationIdKey, context.InvocationId);
                activity.AddTag(NameKey, context.FunctionDefinition.Name);
                activity.AddTag(ProcessIdKey, _processId);
            }

            return activity;
        }
    }
}
