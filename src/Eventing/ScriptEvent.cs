// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FunctionsDotNetWorker
{
    public class ScriptEvent
    {
        public ScriptEvent(string name, string source)
        {
            Name = name;
            Source = source;
        }

        public string Name { get; }

        public string Source { get; }
    }
}
