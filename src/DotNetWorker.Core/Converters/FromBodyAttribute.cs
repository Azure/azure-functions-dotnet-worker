// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Core.Converters;

namespace Microsoft.Azure.Functions.Worker.Http
{
    /// <summary>
    /// Specifies that a parameter should be bound using the request body.
    /// </summary>
    public class FromBodyAttribute : InputConverterAttribute
    {
        /// <summary>
        /// Creates an instance of the <see cref="FromBodyAttribute"/>.
        /// </summary>
        public FromBodyAttribute() 
            : base(typeof(HttpRequestDataConverter))
        {
        }
    }
}
