// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

﻿using System;
using System.Collections.Generic;

namespace Microsoft.Azure.Functions.Worker.Extensions.Abstractions
{
    public interface ISupportCardinality
    {
        /// <summary>
        /// Configures the "cardinality" property
        /// This property indicates that the requested type is an array. Note that for 
        /// inputs and outputs, the effect of cardinality may be different (ex: electing to
        /// receive a collection of events vs. indicating that my return type will be
        /// a collection).
        /// </summary>
        public Cardinality Cardinality { get; set; }
    }

    public enum Cardinality
    {
        Many,
        One
    }
}
