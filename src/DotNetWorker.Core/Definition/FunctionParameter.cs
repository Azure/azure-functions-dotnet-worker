// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

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
        /// <param name="hasDefaultValue">Value that indicates whether the parameter has a default value.</param>
        /// <param name="defaultValue">Default value of the parameter.</param>
        public FunctionParameter(string name, Type type, bool hasDefaultValue, object? defaultValue)
            : this(name, type, hasDefaultValue, defaultValue, ImmutableDictionary<string, object>.Empty)
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
            IsReferenceOrNullableType = !type.IsValueType || Nullable.GetUnderlyingType(type) != null;
            Properties = properties ?? throw new ArgumentNullException(nameof(properties));
        }

        /// <summary>
        /// Creates an instance of the <see cref="FunctionParameter"/> class.
        /// </summary>
        /// <param name="name">The parameter name.</param>
        /// <param name="type">The <see cref="System.Type"/> of the parameter.</param>
        /// <param name="hasDefaultValue">Value that indicates whether the parameter has a default value.</param>
        /// <param name="defaultValue">Default value of the parameter.</param>
        /// <param name="properties">The properties of the parameter.</param>
        public FunctionParameter(string name, Type type, bool hasDefaultValue, object? defaultValue, IReadOnlyDictionary<string, object> properties)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Type = type ?? throw new ArgumentNullException(nameof(type));
            DefaultValue = defaultValue ?? default;
            HasDefaultValue = hasDefaultValue;
            IsReferenceOrNullableType = !type.IsValueType || Nullable.GetUnderlyingType(type) != null;
            Properties = properties ?? throw new ArgumentNullException(nameof(properties));
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
        /// Gets a value that indicates whether this parameter has a default value.
        /// </summary>
        public bool HasDefaultValue { get; }

        /// <summary>
        /// Gets a value indicating whether or not the parameter allows null values.
        /// </summary>
        public bool IsReferenceOrNullableType { get; }

        /// <summary>
        /// Gets the default value of the parameter if exists, else null.
        /// </summary>
        public object? DefaultValue { get; }

        /// <summary>
        /// A dictionary holding properties of this parameter.
        /// </summary>
        public IReadOnlyDictionary<string, object> Properties { get; }
    }
}
