// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Worker.OutputBindings
{
    public class MethodReturnOutputBindingsInfo : OutputBindingsInfo
    {
        private const string ReturnBindingName = "$return";

        private readonly IReadOnlyCollection<string> _bindingNames = new List<string>() { ReturnBindingName };

        public override IReadOnlyCollection<string> BindingNames => _bindingNames;

        public override bool BindDataToDictionary(IDictionary<string, object> dict, object? output)
        {
            if (output is not null)
            {
                dict[ReturnBindingName] = output;
                return true;
            }

            return false;
        }
    }
}
