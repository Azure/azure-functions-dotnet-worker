using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Azure.Functions.Worker.Context.Features;
using Microsoft.Azure.Functions.Worker.OutputBindings;

namespace Microsoft.Azure.Functions.Worker.TestHost
{
    internal class TestFunctionBindingFeature : IFunctionBindingsFeature
    {


        public TestFunctionBindingFeature(IEnumerable<IFunctionBindingsSetup> setups, FunctionContext context)
        {
            TriggerMetadata = ConvertTriggerMetadata(triggerMetadata, context);
            InputData = new Dictionary<string, object?>(inputData);

            OutputBindingData = new Dictionary<string, object?>();

            OutputBindingsInfo = bindingsInfo ?? throw new ArgumentNullException(nameof(bindingsInfo));
        }

        public IReadOnlyDictionary<string, object?> TriggerMetadata { get; }

        public IReadOnlyDictionary<string, object?> InputData { get; }

        public IDictionary<string, object?> OutputBindingData { get; }

        public OutputBindingsInfo OutputBindingsInfo { get; }

        public object? InvocationResult { get; set; }

        private IReadOnlyDictionary<string, object?> ConvertTriggerMetadata(IDictionary<string, object?> triggerMetadata, FunctionContext context)
        {
            var dict = Enumerable.ToDictionary(triggerMetadata, kvp => kvp.Key, kvp => ConvertTriggerValue(kvp.Value, context), StringComparer.OrdinalIgnoreCase);
            return new ReadOnlyDictionary<string, object?>(dict);
        }

        private object? ConvertTriggerValue(object? value, FunctionContext context)
        {
            if (value is HttpRequestDataBuilder builder)
            {
                return builder.Build(context);
            }

            return value;
        }
    }
}
