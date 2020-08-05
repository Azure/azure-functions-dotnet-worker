using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.DotNetWorker.FunctionDescriptor
{
    public interface IFunctionDescriptor
    {
        public string PathToAssembly { get; }
        public string EntryPoint { get; }
        public string TypeName { get; }
        public string FunctionID { get; }
        public string FuncName { get; }
        public MethodInfo FuncMethodInfo { get; }
        public ImmutableArray<ParameterInfo> FuncParamInfo { get; }
        public Type FunctionType { get; }

    }
}
