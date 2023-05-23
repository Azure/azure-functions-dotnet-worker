namespace Microsoft.Azure.Functions.Worker.ApplicationInsights
{
    internal class TraceConstants
    {
        public const string FunctionsActivitySource = "Microsoft.Azure.Functions.Worker";

        public const string AttributeExceptionEventName = "exception";
        public const string AttributeExceptionType = "exception.type";
        public const string AttributeExceptionMessage = "exception.message";
        public const string AttributeExceptionStacktrace = "exception.stacktrace";
    }
}
