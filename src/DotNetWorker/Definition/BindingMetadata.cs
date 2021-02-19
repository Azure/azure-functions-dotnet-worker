// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Worker
{
    public abstract class BindingMetadata
    {
        public abstract string Type { get; set; }

        public abstract Direction Direction { get; set; }
    }

    public enum Direction
    {
        In,
        Out
    };
}
