// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// A type representing the output binding data.
    /// </summary>
    public sealed class OutputBindingData
    {
        internal OutputBindingData(FunctionContext functionContext, string name, object? value, string bindingType)
        {
            _functionContext = functionContext;
            _value = value;
            Name = name;
            BindingType = bindingType;
        }

        private readonly FunctionContext _functionContext;
        private object? _value;

        /// <summary>
        /// Gets the name of the binding entry.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets or sets the value of the binding entry.
        /// </summary>
        public object? Value
        {
            get => _value;
            set
            {
                _value = value;
                _functionContext.GetBindings().OutputBindingData[Name] = value;
            }
        }

        /// <summary>
        /// Gets the type of the binding entry.
        /// Ex: "http","queue" etc.
        /// </summary>
        public string BindingType { get; }
    }
}
