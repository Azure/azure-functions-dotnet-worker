// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Worker.OutputBindings
{
    public class NoOutputBindingsInfo : OutputBindingsInfo
    {
        private static readonly Lazy<NoOutputBindingsInfo> _instance = new(() => new NoOutputBindingsInfo());

        public static NoOutputBindingsInfo Instance => _instance.Value;

        public override IReadOnlyCollection<string> BindingNames => Array.Empty<string>();

        public override bool BindDataToDictionary(IDictionary<string, object> dict, object? output)
        {
            return false;
        }
    }
}
