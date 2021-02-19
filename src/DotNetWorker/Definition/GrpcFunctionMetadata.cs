// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.Azure.WebJobs.Script.Grpc.Messages;

namespace Microsoft.Azure.Functions.Worker.Definition
{
    internal class GrpcFunctionMetadata : FunctionMetadata
    {
        public GrpcFunctionMetadata(FunctionLoadRequest loadRequest)
        {
            EntryPoint = loadRequest.Metadata.EntryPoint;
            Name = loadRequest.Metadata.Name;
            PathToAssembly = Path.GetFullPath(loadRequest.Metadata.ScriptFile);
            FunctionId = loadRequest.FunctionId;

            var grpcbindings = loadRequest.Metadata.Bindings;
            var grpcInputBindings = grpcbindings.Where(kv => kv.Value.Direction == BindingInfo.Types.Direction.In);
            var grpcOutputBindings = grpcbindings.Where(kv => kv.Value.Direction != BindingInfo.Types.Direction.In);

            InputBindings = grpcInputBindings.ToImmutableDictionary(kv => kv.Key, kv => new GrpcBindingMetadata(kv.Value) as BindingMetadata);
            OutputBindings = grpcOutputBindings.ToImmutableDictionary(kv => kv.Key, kv => new GrpcBindingMetadata(kv.Value) as BindingMetadata);
        }

        public override string PathToAssembly { get; set; }

        public override string EntryPoint { get; set; }

        public override string FunctionId { get; set; }

        public override string Name { get; set; }

        public override IImmutableDictionary<string, BindingMetadata> InputBindings { get; set; }

        public override IImmutableDictionary<string, BindingMetadata> OutputBindings { get; set; }
    }
}
