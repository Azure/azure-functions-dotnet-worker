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
        ValueTask<object> ConvertAsync(FunctionContext context, Type targetType);
    }
}
