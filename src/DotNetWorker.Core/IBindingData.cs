// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Worker.Core
{
    /// <summary>
    /// A representation of a Binding Data
    /// </summary>
    public interface IBindingData
    {
        /// <summary>
        /// Gets the properties
        /// </summary>
        IDictionary<string, string> Properties { get; }
    }
}
