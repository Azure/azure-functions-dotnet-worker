// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Functions.Worker.Converters
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
        private readonly Type _inputConverterInterfaceType = typeof(IInputConverter);

        /// <summary>
        /// Stores all input converters.
        /// Key is assembly qualified name of the Converter implementation and value is the instance of it.
        /// </summary>
        private readonly ConcurrentDictionary<string, IInputConverter> _converterCache = new();

        /// <summary>
        /// Stores the default converter instances.
        /// This is an ordered sub set of what is present in _converterCache.
        /// </summary>
        private IReadOnlyList<IInputConverter> _defaultConverters;

        public DefaultInputConverterProvider(IOptions<WorkerOptions> workerOptions, IServiceProvider serviceProvider)
        {
            _workerOptions = workerOptions?.Value ?? throw new ArgumentNullException(nameof(workerOptions));
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
        /// <param name="converterTypeName">The assembly qualified name of the type for which we are requesting an IInputConverter instance.</param>
        /// <exception cref="ArgumentNullException">Throws when the converterTypeName param is null.</exception>
        /// <returns>IConverter instance of the requested type.</returns>
        public IInputConverter GetOrCreateConverterInstance(string converterTypeName)
        {
            if (converterTypeName is null)
            {
                throw new ArgumentNullException((nameof(converterTypeName)));
            }

            // Get from cache or create the instance and cache
            return _converterCache.GetOrAdd(converterTypeName, (key, converterTypeFullName) =>
            {
                // Create the instance and cache that against the assembly qualified name of the type.
                var converterType = Type.GetType(converterTypeFullName);

                if (converterType is null)
                {
                    throw new InvalidOperationException($"Could not create an instance of {converterTypeFullName}");
                }

                ThrowIfTypeCannotBeAssigned(converterType);

                var converterInstance = (IInputConverter)ActivatorUtilities.CreateInstance(_serviceProvider, converterType);
                _converterCache[converterTypeFullName] = converterInstance;

                return converterInstance;

            },converterTypeName);
        }

        /// <summary>
        /// Initializes the converter cache from worker options.
        /// </summary>
        private void InitializeConverterCacheWithDefaultConverters()
        {
            if (_workerOptions.InputConverters is null || _workerOptions.InputConverters.Count == 0)
            {
                throw new InvalidOperationException("No input converters found in worker options!");
            }

            var convertersOrdered = new List<IInputConverter>(_workerOptions.InputConverters.Count);
            
            foreach (Type converterType in _workerOptions.InputConverters)
            {
                ThrowIfTypeCannotBeAssigned(converterType);

                var converterInstance = (IInputConverter)ActivatorUtilities.CreateInstance(_serviceProvider, converterType);

                _converterCache.TryAdd(converterType.AssemblyQualifiedName!, converterInstance);

                // Keep a reference to this instance in an ordered collection so that we can iterate in order
                convertersOrdered.Add(converterInstance);
            }

            _defaultConverters = convertersOrdered;
        }

        /// <summary>
        /// Make sure the converter type is a type which implemented IInputConverter interface 
        /// </summary>
        private void ThrowIfTypeCannotBeAssigned(Type converterType)
        {
            if (!_inputConverterInterfaceType.IsAssignableFrom(converterType))
            {
                throw new InvalidOperationException(
                    $"{converterType.Name} must implement {_inputConverterInterfaceType.FullName} to be used as an input converter");
            }
        }
    }
}
