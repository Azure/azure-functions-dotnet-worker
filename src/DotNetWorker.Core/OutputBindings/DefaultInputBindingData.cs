// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker.Converters;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// A type representing the input binding data.
    /// </summary>
    /// <typeparam name="T">The type of binding data value.</typeparam>
    internal class DefaultInputBindingData<T> : InputBindingData<T>
    {
        private readonly IBindingCache<ConversionResult> _bindingCache;
        private T? _value;
        
        internal DefaultInputBindingData(IBindingCache<ConversionResult> bindingCache, BindingMetadata bindingMetadata, T? value)
        {
            _bindingCache = bindingCache;
            BindingMetadata = bindingMetadata;
            _value = value;
            
        }

        /// <summary>
        /// Gets the binding metadata part of this input binding data instance.
        /// </summary>
        public override BindingMetadata BindingMetadata { get; }

        /// <summary>
        /// Gets or sets the value of the binding result.
        /// </summary>
        public override T? Value
        {
            
            get => _value;
            set
            {
                _value = value;
                _bindingCache[BindingMetadata.Name] = ConversionResult.Success(value);
            }
        }
    }
}
