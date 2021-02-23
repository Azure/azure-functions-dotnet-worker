// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Worker.Tests
{
    public class TestBindingMetadata : BindingMetadata
    {
        public override string Type { get; set; }

        public override BindingDirection Direction { get; set; }
    }
}
