// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker.Tests
{
    public class TestBindingMetadata : BindingMetadata
    {
        public TestBindingMetadata(string type, BindingDirection direction)
        {
            Type = type;
            Direction = direction;
        }

        public override string Type { get; }

        public override BindingDirection Direction { get; }
    }
}
