// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Core.Converters.Converter;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Functions.Worker.Context.Features
{
    /// <summary>
    /// An implementation of <see cref="IInputConverterProvider"/> to get IInputConverter instances.
    ///  - Provides IInputConverter instances from what is defined in WorkerOptions.InputConverters
    ///  - Provides IInputConverter instances when requested for a specific type explicitly.
    ///  - Internally caches the instances created.
    /// </summary>
    internal sealed class DefaultInputConverterProvider : IInputConverterProvider
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly WorkerOptions _workerOptions;       
         
        /// <summary>
        /// Stores all input converters.
        /// </summary>
        private readonly ConcurrentDictionary<Type, IInputConverter> _converterCache = new();

        /// <summary>
        /// Stores the default converter instances.
        /// This is an ordered sub set of what is present in _converterCache.
        /// </summary>
        private IReadOnlyList<IInputConverter> _defaultConverters;

        public DefaultInputConverterProvider(IOptions<WorkerOptions> workerOptions, IServiceProvider serviceProvider)
        {
            _workerOptions = workerOptions.Value ?? throw new ArgumentNullException(nameof(workerOptions));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            
            InitializeConverterCacheWithDefaultConverters();
        }
                
        /// <summary>
        /// Gets an ordered collection of default converter instances.
        /// </summary>
        public IEnumerable<IInputConverter> DefaultConverters => _defaultConverters;
                
        /// <summary>
        /// Gets an instance of the converter for the type requested.
        /// </summary>
        /// <param name="converterType">The type of IConverter implementation to return.</param>
        /// <returns>IConverter instance of the requested type.</returns>
        public IInputConverter GetOrCreateConverterInstance(Type converterType)
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
                // Create the instance and cache.
                converterInstance = (IInputConverter)ActivatorUtilities.CreateInstance(_serviceProvider, converterType);
                _converterCache[converterType] = converterInstance;
            }

            return converterInstance;
        }

        /// <summary>
        /// Initializes the defaultConverter cache from worker options.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        private void InitializeConverterCacheWithDefaultConverters()
        {
            if (_workerOptions.InputConverters == null || _workerOptions.InputConverters.Count == 0)
            {
                throw new InvalidOperationException("No input converters found in worker options!");
            }

            var interfaceType = typeof(IInputConverter);
            var convertersOrdered = new List<IInputConverter>(_workerOptions.InputConverters.Count);
            
            foreach (Type converterType in _workerOptions.InputConverters)
            {
                if (!interfaceType.IsAssignableFrom(converterType))
                {
                    throw new InvalidOperationException($"{converterType.Name} must implement {interfaceType.FullName} to be used as an input converter");
                }

                var converterInstance = (IInputConverter)ActivatorUtilities.CreateInstance(_serviceProvider, converterType);

                _converterCache.TryAdd(converterType, converterInstance);
                
                // Keep a reference to this instance in an ordered collection so that we can iterate in order
                convertersOrdered.Add(converterInstance);
            }

            _defaultConverters = convertersOrdered;
        }
    }
}
