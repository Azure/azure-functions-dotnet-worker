// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Worker.OutputBindings
{
    internal class EmptyOutputBindingsInfo : OutputBindingsInfo
    {
        private static readonly Lazy<EmptyOutputBindingsInfo> _instance = new(() => new EmptyOutputBindingsInfo());

        public static EmptyOutputBindingsInfo Instance => _instance.Value;

        private EmptyOutputBindingsInfo()
        {
        }

        public override void BindOutputInContext(FunctionContext context)
        {
        }
    }
}
