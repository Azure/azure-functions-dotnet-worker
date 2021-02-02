// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Azure.Functions.Worker.Definition;

namespace Microsoft.Azure.Functions.Worker
{
    internal class DefaultOutputBinding<T> : OutputBinding<T>
    {
        private readonly FunctionParameter _param;
        private readonly IDictionary<string, object> _outputBindings;

        public DefaultOutputBinding(FunctionParameter param, IDictionary<string, object> outputBindings)
        {
            _param = param;
            _outputBindings = outputBindings;
        }

        public override void SetValue(T value)
        {
            _outputBindings[_param.Name] = value;
        }

        internal override T GetValue()
        {
            return (T)_outputBindings[_param.Name];
        }
    }
}
