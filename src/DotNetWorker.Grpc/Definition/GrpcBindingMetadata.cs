// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker.Grpc.Messages;

namespace Microsoft.Azure.Functions.Worker
{
    internal class GrpcBindingMetadata : BindingMetadata
    {
        public GrpcBindingMetadata(string name, BindingInfo bindingInfo)
        {
            Name = name;
            Type = bindingInfo.Type;
            Direction = bindingInfo.Direction == BindingInfo.Types.Direction.In ? BindingDirection.In : BindingDirection.Out;
        }

        public override string Name { get; }

        public override string Type { get; }

        public override BindingDirection Direction { get; }
    }
}
