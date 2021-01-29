using System.Collections.Immutable;
using System.Reflection;

namespace Microsoft.Azure.Functions.Worker
{
    public class FunctionMetadata
    {
        public string? PathToAssembly { get; set; }

        public string? EntryPoint { get; set; }

        public string? TypeName { get; set; }

        public string? FunctionId { get; set; }

        public string? FuncName { get; set; }        
    }
}
