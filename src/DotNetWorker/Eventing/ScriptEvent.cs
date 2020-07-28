// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.DotNetWorker
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
