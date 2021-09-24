// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Functions.Worker.Context.Features
{
    /// <summary>
    /// An implementation of <see cref="IInputConverterProvider"/> to get IFunctionInputConverter instances.
    ///  - Provides IFunctionInputConverter instances from what is defined in WorkerOptions.FunctionInputConverters
    ///  - Provides IFunctionInputConverter instances when requested for a specific type explicitly.
    ///  - Internally caches the instances created.
    /// </summary>
    internal sealed class DefaultInputConverterProvider : IInputConverterProvider
    {
        private readonly ConcurrentDictionary<Type, IInputConverter> _converterCache = new();
        private readonly IServiceProvider _serviceProvider;
        private readonly WorkerOptions _workerOptions;
        
        public DefaultInputConverterProvider(IOptions<WorkerOptions> workerOptions, IServiceProvider serviceProvider)
        {
            _workerOptions = workerOptions.Value ?? throw new ArgumentNullException(nameof(workerOptions));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            DefaultConverters = CreateDefaultConverters();
        }
                
        /// <summary>
        /// Gets the built-in default converters.
        /// </summary>
        public IEnumerable<IInputConverter> DefaultConverters { get; }
                
        /// <summary>
        /// Gets an instance of the converter for the type requested.
        /// </summary>
        /// <param name="converterType">The type of IConverter implementation to return.</param>
        /// <returns>IConverter instance of the requested type.</returns>
        public IInputConverter GetConverterInstance(Type converterType)
        {
            if (converterType == null)
            {
                throw new ArgumentNullException((nameof(converterType)));
            }

            IInputConverter converterInstance;
            
            // Get the IConverter instance for converterType from cache if present.
            if (_converterCache.TryGetValue(converterType, out var converterFromCache))
            {
                converterInstance = converterFromCache;
            }
            else
            {
                // Create and cache.
                converterInstance = (IInputConverter)ActivatorUtilities.CreateInstance(_serviceProvider, converterType);
                _converterCache[converterType] = converterInstance;
            }

            return converterInstance;
        }
        
        private IEnumerable<IInputConverter> CreateDefaultConverters()
        {
            if (_workerOptions.InputConverters == null || _workerOptions.InputConverters.Count == 0)
            {
                throw new InvalidOperationException("No binding converters found in worker options!");
            }

            var converterList = new List<IInputConverter>(_workerOptions.InputConverters.Count);

            var interfaceType = typeof(IInputConverter);
            foreach (Type converterType in _workerOptions.InputConverters)
            {
                if (!interfaceType.IsAssignableFrom(converterType))
                {
                    throw new InvalidOperationException($"{converterType.Name} must implement {interfaceType.FullName} to be used as an input converter");
                }

                converterList.Add((IInputConverter)ActivatorUtilities.CreateInstance(_serviceProvider, converterType));
            }

            return converterList;
        }
    }
}
