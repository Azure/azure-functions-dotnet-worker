// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Azure.Functions.Worker.Context.Features;

namespace Microsoft.Azure.Functions.Worker
{
    internal sealed class DefaultBindingContext : BindingContext
    {
        private readonly FunctionContext _functionContext;
        private IFunctionBindingsFeature? _functionBindings;

        public DefaultBindingContext(FunctionContext functionContext)
        {
            _functionContext = functionContext ?? throw new ArgumentNullException(nameof(functionContext));
        }

        public override IReadOnlyDictionary<string, object?> BindingData
            => (_functionBindings ??= _functionContext.Features.GetRequired<IFunctionBindingsFeature>()).TriggerMetadata;
    }
}
