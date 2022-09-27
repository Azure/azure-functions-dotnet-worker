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
        /// Version of ParameterBindingData schema
        /// </summary>
        string Version { get; }

        /// <summary>
        /// Content type
        /// </summary>
        string ContentType { get; }

        /// <summary>
        /// An object containing any required information to hydrate
        /// an SDK-type object in the out-of-process worker
        /// </summary>
        object Content { get; }
    }
}
