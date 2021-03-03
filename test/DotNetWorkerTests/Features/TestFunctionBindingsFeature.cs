using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Azure.Functions.Worker.Context.Features;
using Microsoft.Azure.Functions.Worker.OutputBindings;

namespace Microsoft.Azure.Functions.Worker.Tests.Features
{
    internal class TestFunctionBindingsFeature : IFunctionBindingsFeature
    {
        public IReadOnlyDictionary<string, object> TriggerMetadata { get; init; } = new ReadOnlyDictionary<string, object>(new Dictionary<string, object>());

        public IReadOnlyDictionary<string, object> InputData { get; init; } = new ReadOnlyDictionary<string, object>(new Dictionary<string, object>());

        public OutputBindingsInfo OutputBindings { get; init; } = EmptyOutputBindingsInfo.Instance;
    }
}
