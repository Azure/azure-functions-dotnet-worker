// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Azure.Functions.Worker.OutputBindings;

namespace Microsoft.Azure.Functions.Worker.Context.Features
{
    internal interface IFunctionBindingsFeature
    {
        public IReadOnlyDictionary<string, object?> TriggerMetadata { get; }

        public IReadOnlyDictionary<string, object?> InputData { get; }

        public IDictionary<string, object?> OutputBindingData { get; }

        public OutputBindingsInfo OutputBindingsInfo { get; }

        public object? InvocationResult { get; set; }
    }
}
