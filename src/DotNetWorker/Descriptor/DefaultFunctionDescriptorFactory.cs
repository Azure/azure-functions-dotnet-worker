using Microsoft.Azure.WebJobs.Script.Grpc.Messages;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.DotNetWorker.Descriptor
{
    class DefaultFunctionDescriptorFactory : IFunctionDescriptorFactory
    {
        public FunctionDescriptor Create(FunctionLoadRequest request)
        {
            FunctionDescriptor descriptor = new FunctionDescriptor();
            descriptor.PathToAssembly = request.Metadata.ScriptFile;
            descriptor.EntryPoint = request.Metadata.EntryPoint;
            var assemblyName = AssemblyName.GetAssemblyName(descriptor.PathToAssembly).Name;
            descriptor.FuncName = request.Metadata.Name;
            descriptor.TypeName = assemblyName + "." + descriptor.FuncName;
            descriptor.FunctionId = request.FunctionId;
            descriptor.FunctionType = Assembly.GetEntryAssembly().GetType(descriptor.TypeName);
            descriptor.FuncMethodInfo = descriptor.FunctionType.GetMethod("Run");
            descriptor.FuncParamInfo = descriptor.FuncMethodInfo.GetParameters().ToImmutableArray<ParameterInfo>();
            return descriptor;
        }
    }
}
