// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Security.Cryptography.X509Certificates;
using Microsoft.CodeAnalysis;

namespace Microsoft.Azure.Functions.Worker.Sdk.Generators
{
    internal readonly struct KnownFunctionMetadataTypes
    {
        private readonly Lazy<INamedTypeSymbol?> _bindingAttribute;
        private readonly Lazy<INamedTypeSymbol?> _outputBindingAttribute;
        private readonly Lazy<INamedTypeSymbol?> _functionName;
        private readonly Lazy<INamedTypeSymbol?> _bindingPropertyNameAttribute;
        private readonly Lazy<INamedTypeSymbol?> _defaultValue;
        private readonly Lazy<INamedTypeSymbol?> _httpResponseData;
        private readonly Lazy<INamedTypeSymbol?> _httpTriggerBinding;
        private readonly Lazy<INamedTypeSymbol?> _retryAttribute;
        private readonly Lazy<INamedTypeSymbol?> _bindingCapabilitiesAttribute;
        private readonly Lazy<INamedTypeSymbol?> _fixedDelayRetryAttribute;
        private readonly Lazy<INamedTypeSymbol?> _exponentialBackoffRetryAttribute;
        private readonly Lazy<INamedTypeSymbol?> _inputConverterAttributeType;
        private readonly Lazy<INamedTypeSymbol?> _supportedTargetTypeAttributeType;
        private readonly Lazy<INamedTypeSymbol?> _supportsDeferredBindingAttributeType;
        private readonly Lazy<INamedTypeSymbol?> _httpResultAttribute;

        internal KnownFunctionMetadataTypes(Compilation compilation)
        {
            _bindingAttribute = new Lazy<INamedTypeSymbol?>(() => compilation.GetTypeByMetadataName(Constants.Types.BindingAttribute)); // TODO: Find appropriate exception and/or error message.
            _outputBindingAttribute = new Lazy<INamedTypeSymbol?>(() => compilation.GetTypeByMetadataName(Constants.Types.OutputBindingAttribute));
            _functionName = new Lazy<INamedTypeSymbol?>(() => compilation.GetTypeByMetadataName(Constants.Types.FunctionName));
            _bindingPropertyNameAttribute = new Lazy<INamedTypeSymbol?>(() => compilation.GetTypeByMetadataName(Constants.Types.BindingPropertyNameAttribute));
            _defaultValue = new Lazy<INamedTypeSymbol?>(() => compilation.GetTypeByMetadataName(Constants.Types.DefaultValue));
            _httpResponseData = new Lazy<INamedTypeSymbol?>(() => compilation.GetTypeByMetadataName(Constants.Types.HttpResponseData));
            _httpTriggerBinding = new Lazy<INamedTypeSymbol?>(() => compilation.GetTypeByMetadataName(Constants.Types.HttpTriggerBinding));
            _retryAttribute = new Lazy<INamedTypeSymbol?>(() => compilation.GetTypeByMetadataName(Constants.Types.RetryAttribute));
            _bindingCapabilitiesAttribute = new Lazy<INamedTypeSymbol?>(() => compilation.GetTypeByMetadataName(Constants.Types.BindingCapabilitiesAttribute));
            _fixedDelayRetryAttribute = new Lazy<INamedTypeSymbol?>(() => compilation.GetTypeByMetadataName(Constants.Types.FixedDelayRetryAttribute));
            _exponentialBackoffRetryAttribute = new Lazy<INamedTypeSymbol?>(() => compilation.GetTypeByMetadataName(Constants.Types.ExponentialBackoffRetryAttribute));
            _inputConverterAttributeType = new Lazy<INamedTypeSymbol?>(() => compilation.GetTypeByMetadataName(Constants.Types.InputConverterAttributeType));
            _supportedTargetTypeAttributeType = new Lazy<INamedTypeSymbol?>(() => compilation.GetTypeByMetadataName(Constants.Types.SupportedTargetTypeAttributeType));
            _supportsDeferredBindingAttributeType = new Lazy<INamedTypeSymbol?>(() => compilation.GetTypeByMetadataName(Constants.Types.SupportsDeferredBindingAttributeType));
            _httpResultAttribute = new Lazy<INamedTypeSymbol?>(() => compilation.GetTypeByMetadataName(Constants.Types.HttpResultAttribute));
        }

        public INamedTypeSymbol? BindingAttribute { get => _bindingAttribute.Value; }

        public INamedTypeSymbol? OutputBindingAttribute { get => _outputBindingAttribute.Value; }

        public INamedTypeSymbol? FunctionName {  get => _functionName.Value; }

        public INamedTypeSymbol? BindingPropertyNameAttribute { get => _bindingPropertyNameAttribute.Value; }

        public INamedTypeSymbol? DefaultValue { get => _defaultValue.Value; }

        public INamedTypeSymbol? HttpResponseData { get => _httpResponseData.Value; }

        public INamedTypeSymbol? HttpResultAttribute { get => _httpResultAttribute.Value; }

        public INamedTypeSymbol? HttpTriggerBinding { get => _httpTriggerBinding.Value; }

        public INamedTypeSymbol? RetryAttribute { get => _retryAttribute.Value;  }

        public INamedTypeSymbol? BindingCapabilitiesAttribute { get => _bindingCapabilitiesAttribute.Value; }

        public INamedTypeSymbol? FixedDelayRetryAttribute {  get => _fixedDelayRetryAttribute.Value; }

        public INamedTypeSymbol? ExponentialBackoffRetryAttribute { get => _exponentialBackoffRetryAttribute.Value; }

        public INamedTypeSymbol? InputConverterAttributeType { get => _inputConverterAttributeType.Value; }

        public INamedTypeSymbol? SupportedTargetTypeAttributeType { get => _supportedTargetTypeAttributeType.Value; }

        public INamedTypeSymbol? SupportsDeferredBindingAttributeType { get => _supportsDeferredBindingAttributeType.Value; }
    }
}
