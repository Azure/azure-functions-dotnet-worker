// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker
{
    internal sealed class DefaultOutputBindingData<T> : OutputBindingData<T>
    {
        private readonly FunctionContext _functionContext;
        private T? _value;

        internal DefaultOutputBindingData(FunctionContext functionContext, string name, T? value, string bindingType)
        {
            _functionContext = functionContext;
            _value = value;
            Name = name;
            BindingType = bindingType;
        }

        public override string BindingType { get; }

        public override string Name { get; }

        public override T? Value
        {
            get => _value;
            set
            {
                _value = value;
                _functionContext.GetBindings().OutputBindingData[Name] = value;
            }
        }
    }
}
