// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Worker.OutputBindings
{
    /// <summary>
    /// Encapsulates the information about all output bindings in a Function
    /// </summary>
    public abstract class OutputBindingsInfo
    {
        /// <summary>
        /// Gets the name of all the output bindings defined for a function
        /// </summary>
        public abstract IReadOnlyCollection<string> BindingNames { get; }

        /// <summary>
        /// Binds <paramref name="data"/> from a function execution to <paramref name="dict"/>
        /// </summary>
        /// <param name="dict">The dicitionary to bind the data to.</param>
        /// <param name="data">The data to bind to the dictionary.</param>
        /// <returns>boolean to indiciate if data was binded to dictionary</returns>
        public abstract bool BindDataToDictionary(IDictionary<string, object> dict, object? data);
    }
}
