// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

﻿using System;
using System.Collections.Generic;

namespace Microsoft.Azure.Functions.Worker.Extensions.Abstractions
{
    public interface ISupportCardinality
    {
        /// <summary>
        /// Configures trigger to process events in batches or one at a time.
        /// This translates to values for the "cardinality" property in WebJobs terms.
        ///    true => "Many"
        ///    false => "One"
        ///    
        /// To default to a particular true or false, add the "DefaultValue" attribute to the method.
        /// </summary>
        public Cardinality Cardinality { get; set; }
    }

    public enum Cardinality
    {
        Many,
        One
    }
}
