// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

﻿using System;

namespace Microsoft.Azure.Functions.Worker.Extensions.Abstractions
{
    public interface IBatchedInput
    {
        /// <summary>
        /// Configures trigger to process events in batches or one at a time.
        /// This translates to values for the "cardinality" property in WebJobs terms.
        ///    true => "Many"
        ///    false => "One"
        ///    
        /// To default to a particular true or false, the constructor of the attribute that inherits
        /// from this must include 'bool isBatched = [true / false]' as an optional parameters
        /// on the constructor
        /// </summary>
        public bool IsBatched { get; set; }
    }
}
