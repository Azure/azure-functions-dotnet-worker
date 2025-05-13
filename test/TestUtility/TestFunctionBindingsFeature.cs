// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Azure.Functions.Worker.Context.Features;
using Microsoft.Azure.Functions.Worker.OutputBindings;

namespace Microsoft.Azure.Functions.Worker.Tests
{
    internal class TestFunctionBindingsFeature : IFunctionBindingsFeature
    {
        public IReadOnlyDictionary<string, object> TriggerMetadata { get; init; } = new ReadOnlyDictionary<string, object>(new Dictionary<string, object>());

        public IReadOnlyDictionary<string, object> InputData { get; init; } = new ReadOnlyDictionary<string, object>(new Dictionary<string, object>());

        public IDictionary<string, object> OutputBindingData { get; } = new Dictionary<string, object>();

        public OutputBindingsInfo OutputBindingsInfo { get; init; } = EmptyOutputBindingsInfo.Instance;

        public object InvocationResult { get; set; }
    }
}
