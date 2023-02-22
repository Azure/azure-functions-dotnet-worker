﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Worker.Context.Features
{
    /// <summary>
    /// Provides a mechanism to bind function inputs.
    /// </summary>
    public interface IFunctionInputBindingFeature
    {
        /// <summary>
        /// Asynchronously binds the function inputs.
        /// </summary>
        /// <param name="context">The <see cref="FunctionContext"/> instance.</param>
        /// <returns>A <see cref="ValueTask{TResult}"/>that will contain an instance of the <see cref="FunctionInputBindingResult"/>.</returns>
        ValueTask<FunctionInputBindingResult> BindFunctionInputAsync(FunctionContext context);
    }
}
