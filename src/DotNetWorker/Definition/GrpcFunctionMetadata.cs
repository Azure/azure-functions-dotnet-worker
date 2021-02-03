using System.IO;
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
        }

        public override string PathToAssembly { get; set; }

        public override string EntryPoint { get; set; }

        public override string FunctionId { get; set; }

        public override string Name { get; set; }
    }
}
