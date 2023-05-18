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
        private readonly Lazy<INamedTypeSymbol?> _httpResponse;
        private readonly Lazy<INamedTypeSymbol?> _httpTriggerBinding;
        private readonly Lazy<INamedTypeSymbol?> _retryAttribute;
        private readonly Lazy<INamedTypeSymbol?> _bindingCapabilitiesAttribute;
        private readonly Lazy<INamedTypeSymbol?> _fixedDelayRetryAttribute;
        private readonly Lazy<INamedTypeSymbol?> _exponentialBackoffRetryAttribute;

        internal KnownFunctionMetadataTypes(Compilation compilation)
        {
            _bindingAttribute = new Lazy<INamedTypeSymbol?>(() => compilation.GetTypeByMetadataName(Constants.Types.BindingAttribute)); // TODO: Find appropriate exception and/or error message.
            _outputBindingAttribute = new Lazy<INamedTypeSymbol?>(() => compilation.GetTypeByMetadataName(Constants.Types.OutputBindingAttribute));
            _functionName = new Lazy<INamedTypeSymbol?>(() => compilation.GetTypeByMetadataName(Constants.Types.FunctionName));
            _bindingPropertyNameAttribute = new Lazy<INamedTypeSymbol?>(() => compilation.GetTypeByMetadataName(Constants.Types.BindingPropertyNameAttribute));
            _defaultValue = new Lazy<INamedTypeSymbol?>(() => compilation.GetTypeByMetadataName(Constants.Types.DefaultValue));
            _httpResponse = new Lazy<INamedTypeSymbol?>(() => compilation.GetTypeByMetadataName(Constants.Types.HttpResponse));
            _httpTriggerBinding = new Lazy<INamedTypeSymbol?>(() => compilation.GetTypeByMetadataName(Constants.Types.HttpTriggerBinding));
            _retryAttribute = new Lazy<INamedTypeSymbol?>(() => compilation.GetTypeByMetadataName(Constants.Types.RetryAttribute));
            _bindingCapabilitiesAttribute = new Lazy<INamedTypeSymbol?>(() => compilation.GetTypeByMetadataName(Constants.Types.BindingCapabilitiesAttribute));
            _fixedDelayRetryAttribute = new Lazy<INamedTypeSymbol?>(() => compilation.GetTypeByMetadataName(Constants.Types.FixedDelayRetryAttribute));
            _exponentialBackoffRetryAttribute = new Lazy<INamedTypeSymbol?>(() => compilation.GetTypeByMetadataName(Constants.Types.ExponentialBackoffRetryAttribute));

        }

        public INamedTypeSymbol? BindingAttribute { get => _bindingAttribute.Value; }

        public INamedTypeSymbol? OutputBindingAttribute { get => _outputBindingAttribute.Value; }

        public INamedTypeSymbol? FunctionName {  get => _functionName.Value; }

        public INamedTypeSymbol? BindingPropertyNameAttribute { get => _bindingPropertyNameAttribute.Value; }

        public INamedTypeSymbol? DefaultValue { get => _defaultValue.Value; }

        public INamedTypeSymbol? HttpResponse { get => _httpResponse.Value; }

        public INamedTypeSymbol? HttpTriggerBinding { get => _httpTriggerBinding.Value; }

        public INamedTypeSymbol? RetryAttribute { get => _retryAttribute.Value;  }

        public INamedTypeSymbol? BindingCapabilitiesAttribute { get => _bindingCapabilitiesAttribute.Value; }

        public INamedTypeSymbol? FixedDelayRetryAttribute {  get => _fixedDelayRetryAttribute.Value; }

        public INamedTypeSymbol? ExponentialBackoffRetryAttribute { get => _exponentialBackoffRetryAttribute.Value; }
    }
}
