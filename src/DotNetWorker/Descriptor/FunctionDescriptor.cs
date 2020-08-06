using System;
using System.Collections.Immutable;
using System.Reflection;

namespace Microsoft.Azure.Functions.DotNetWorker.Descriptor
{
    public class FunctionDescriptor
    {
        public string PathToAssembly { get; set; }

        public string EntryPoint { get; set; }

        public string TypeName { get; set; }

        public string FunctionId { get; set; }

        public string FuncName { get; set; }

        public MethodInfo FuncMethodInfo { get; set; }

        public ImmutableArray<ParameterInfo> FuncParamInfo { get; set; }

        public Type FunctionType { get; set; }
    }
}
