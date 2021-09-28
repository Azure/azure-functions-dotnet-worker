// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Azure.Functions.Worker.Converters
{
    /// <summary>
    /// A type defining the information needed for an input conversion operation.
    /// </summary>
    public sealed class DefaultConverterContext : ConverterContext
    {
        /// <summary>
        /// Creates a new instance of <see cref="DefaultConverterContext"/>
        /// </summary>
        /// <param name="targetType">The target type.</param>
        /// <param name="source">The source.</param>
        /// <param name="context">The function context.</param>
        public DefaultConverterContext(Type targetType, object? source, FunctionContext context)
        {
            TargetType = targetType ?? throw new ArgumentNullException(nameof(context));
            FunctionContext = context ?? throw new ArgumentNullException(nameof(context));
            Source = source;
        }
                
        /// <summary>
        /// The target type to which conversion should happen.
        /// </summary>
        /// 
        public override Type TargetType { get; set; }
                
        /// <summary>
        /// The source data used for conversion.
        /// </summary>
        public override object? Source { get; set; }
                
        /// <summary>
        /// The function context.
        /// </summary>
        public override FunctionContext FunctionContext { get; set; }
                
        /// <summary>
        /// Dictionary for additional meta information used for conversion.
        /// </summary>
        public override IReadOnlyDictionary<string, object>? Properties { get; set;}
    }
}
