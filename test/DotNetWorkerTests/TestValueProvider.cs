// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Azure.Functions.Worker.Context;

namespace Microsoft.Azure.Functions.Worker.Tests
{
    public class TestValueProvider : IValueProvider
    {
        private readonly IDictionary<string, object> _values;

        public TestValueProvider(IDictionary<string, object> values)
        {
            _values = values ?? throw new ArgumentNullException(nameof(values));
        }

        public object GetValue(string name, FunctionContext functionContext)
        {
            _values.TryGetValue(name, out object value);

            return value;
        }
    }
}
