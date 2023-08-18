// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Worker.Extensions.Http.Converters
{
    /// <summary>
    /// Defines an interface for model binding conversion used when binding to
    /// the body of an HTTP request.
    /// </summary>
    public interface IFromBodyConversionFeature
    {
        /// <summary>
        /// Converts the body of an HTTP request to the specified type.
        /// </summary>
        /// <param name="context">The <see cref="FunctionContext"/> for the invocation.</param>
        /// <param name="targetType">The target type for the conversion.</param>
        /// <returns>A <see cref="ValueTask{TResult}"/> that completes when the conversion is finished.</returns>
        ValueTask<object?> ConvertAsync(FunctionContext context, Type targetType);
    }
}
