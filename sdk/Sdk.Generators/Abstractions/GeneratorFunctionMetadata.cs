﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq.Expressions;

namespace Microsoft.Azure.Functions.Worker.Sdk.Generators
{
    internal class GeneratorFunctionMetadata
    {
        public string? Name { get; set; }

        public string? ScriptFile { get; set; }

        public string? FunctionId { get; set; }

        public bool IsProxy { get; set; } = false;

        public string? EntryPoint { get; set; }

        public string? Language { get; set; }

        public bool IsHttpTrigger { get; set; } = false;

        public IDictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

        public IList<IDictionary<string, string>> RawBindings { get; set; } = new List<IDictionary<string, string>>(); // List of <propertyName, propertyValue> bindings.
    }
}
