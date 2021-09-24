﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker.Context.Features
{
    /// <summary>
    /// Provider to get IInputConversionFeature instance.
    /// </summary>
    internal sealed class DefaultInputConversionFeatureProvider : IInputConversionFeatureProvider
    {
        private static readonly Type _featureType = typeof(DefaultInputConversionFeature);
        private readonly IInputConverterProvider _inputConverterProvider;

        public DefaultInputConversionFeatureProvider(IInputConverterProvider inputConverterProvider)
        {
            _inputConverterProvider = inputConverterProvider;
        }

        public bool TryCreate(Type type, out IInputConversionFeature? feature)
        {
            feature = type == _featureType 
                ? new DefaultInputConversionFeature(_inputConverterProvider) 
                : null;

            return feature is not null;
        }
    }
}
