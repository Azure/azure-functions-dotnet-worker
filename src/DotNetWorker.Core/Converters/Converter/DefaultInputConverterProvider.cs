// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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

        public DefaultInputConverterProvider(IOptions<WorkerOptions> workerOptions, IServiceProvider serviceProvider)
        {
            _workerOptions = workerOptions?.Value ?? throw new ArgumentNullException(nameof(workerOptions));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            if (!_workerOptions.InputConverters.Any())
            {
                throw new InvalidOperationException("No input converters found in worker options.");
            }
        }

        /// <summary>
        /// Get a collection of registered converter instances.
        /// </summary>
        public IEnumerable<IInputConverter> RegisteredInputConverters
        {
            get
            {
                foreach (var converterType in _workerOptions.InputConverters!)
                {
                    yield return _converterCache.GetOrAdd(converterType.AssemblyQualifiedName!, (key) =>
                    {
                        return (IInputConverter)ActivatorUtilities.CreateInstance(_serviceProvider, converterType);
                    });
                }
            }
        }

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
            return _converterCache.GetOrAdd(converterTypeName, (key, converterTypeAssemblyQualifiedName) =>
            {
                // Create the instance and cache that against the assembly qualified name of the type.
                var converterType = Type.GetType(converterTypeAssemblyQualifiedName);

                if (converterType is null)
                {
                    throw new InvalidOperationException($"Could not create an instance of {converterTypeAssemblyQualifiedName}.");
                }

                EnsureTypeCanBeAssigned(converterType);

                return (IInputConverter)ActivatorUtilities.CreateInstance(_serviceProvider, converterType);

            }, converterTypeName);
        }

        /// <summary>
        /// Make sure the converter type is a type which has implemented <see cref="IInputConverter"/> interface 
        /// </summary>
        private void EnsureTypeCanBeAssigned(Type converterType)
        {
            if (!_inputConverterInterfaceType.IsAssignableFrom(converterType))
            {
                throw new InvalidOperationException(
                    $"{converterType.Name} must implement {_inputConverterInterfaceType.FullName} to be used as an input converter.");
            }
        }
    }
}
