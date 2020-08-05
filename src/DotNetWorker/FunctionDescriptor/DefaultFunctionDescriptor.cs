using System;
using System.Collections.Immutable;
using System.Reflection;
using Microsoft.Azure.Functions.DotNetWorker.FunctionDescriptor;
using Microsoft.Azure.WebJobs.Script.Grpc.Messages;

namespace Microsoft.Azure.Functions.DotNetWorker
{
    internal class DefaultFunctionDescriptor : IFunctionDescriptor
    {
        private string assemblyName;

        public DefaultFunctionDescriptor(FunctionLoadRequest funcLoadRequest)
        {
            PathToAssembly = funcLoadRequest.Metadata.ScriptFile;
            EntryPoint = funcLoadRequest.Metadata.EntryPoint;
            assemblyName = AssemblyName.GetAssemblyName(PathToAssembly).Name;
            FuncName = funcLoadRequest.Metadata.Name;
            TypeName = assemblyName + "." + FuncName;
            FunctionID = funcLoadRequest.FunctionId;

            FunctionType = Assembly.GetEntryAssembly().GetType(TypeName);
            FuncMethodInfo = FunctionType.GetMethod("Run");
            FuncParamInfo = FuncMethodInfo.GetParameters().ToImmutableArray<ParameterInfo>();

        }

        public string PathToAssembly { get; private set; }

        public string EntryPoint { get; private set; }

        public string TypeName { get; private set; }

        public string FunctionID { get; private set; }

        public string FuncName { get; private set; }

        public MethodInfo FuncMethodInfo { get; private set; }

        public ImmutableArray<ParameterInfo> FuncParamInfo { get; private set; }

        public Type FunctionType { get; private set; }
    }
}
