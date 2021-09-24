// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Microsoft.Azure.Functions.Worker.Core.Converters.Converter;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Represents a parameter defined by the target function.
    /// </summary>
    public class FunctionParameter
    {
        /// <summary>
        /// Creates an instance of the <see cref="FunctionParameter"/> class.
        /// </summary>
        /// <param name="name">The parameter name.</param>
        /// <param name="type">The <see cref="System.Type"/> of the parameter.</param>
        public FunctionParameter(string name, Type type)
            : this(name, type, ImmutableDictionary<string, object>.Empty)
        {
        }

        /// <summary>
        /// Creates an instance of the <see cref="FunctionParameter"/> class.
        /// </summary>
        /// <param name="name">The parameter name.</param>
        /// <param name="type">The <see cref="System.Type"/> of the parameter.</param>
        /// <param name="properties">The properties of the parameter.</param>
        public FunctionParameter(string name, Type type, IReadOnlyDictionary<string, object> properties)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Type = type ?? throw new ArgumentNullException(nameof(type));
            Properties = properties ?? throw new ArgumentNullException(nameof(properties));
        }

        /// <summary>
        /// Creates an instance of the <see cref="FunctionParameter"/> class.
        /// </summary>
        /// <param name="name">The parameter name.</param>
        /// <param name="type">The <see cref="System.Type"/> of the parameter.</param>
        /// <param name="customAttributes">The custom attributes associated with this parameter.</param>
        public FunctionParameter(string name, Type type, IEnumerable<CustomAttributeData> customAttributes) : this(name, type)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Type = type ?? throw new ArgumentNullException(nameof(type));
            Properties = ImmutableDictionary<string, object>.Empty;

            var bindingConverterAttributeData = customAttributes.FirstOrDefault(a => a.AttributeType == typeof(InputConverterAttribute));
            if (bindingConverterAttributeData != null)
            {
                CustomAttributeTypedArgument customConverter = bindingConverterAttributeData.ConstructorArguments
                                                                    .FirstOrDefault(arg => arg.ArgumentType == typeof(Type));

                this.BindingConverterType = (Type)customConverter.Value!;

            }
        }

        /// <summary>
        /// Gets the parameter name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the parameter <see cref="System.Type"/>.
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// A dictionary holding properties of this parameter.
        /// </summary>
        public IReadOnlyDictionary<string, object> Properties { get; }

        // TO DO : Move this to Properties above
        /// <summary>
        /// Binding converter type associated with this parameter. Optional.
        /// </summary>
        public Type? BindingConverterType { get; }
    }
}
