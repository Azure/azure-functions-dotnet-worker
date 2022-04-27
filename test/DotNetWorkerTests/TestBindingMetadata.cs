// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker.Tests
{
    public class TestBindingMetadata : BindingMetadata
    {
        public TestBindingMetadata(string name, string type, BindingDirection direction)
        {
            Name = name;
            Type = type;
            Direction = direction;
        }

        public override string Name { get; }

        public override string Type { get; }

        public override BindingDirection Direction { get; }
    }
}
