namespace Microsoft.Azure.Functions.Worker.TestHost
{
    public static class InvocationContextExtensions
    {
        public static InvocationContext WithHttpTrigger(this InvocationContext context, string parameterName, HttpRequestDataBuilder requestBuilder)
        {
            context.TriggerPayload[parameterName] = requestBuilder;
            return context;
        }

        public static InvocationContext WithInputData(this InvocationContext context, string parameterName, object value)
        {
            context.InputBindingsPayload[parameterName] = value;
            return context;
        }
    }
}
