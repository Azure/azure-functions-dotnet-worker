using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Worker.Core.FunctionMetadata
{
    internal interface IFunctionMetadata
    {
        string FunctionId { get; set; }
        bool IsProxy { get; set; }
        string Language { get; set; }
        bool ManagedDependencyEnabled { get; set; }
        string Name { get; set; }
        IEnumerable<string> RawBindings { get; }
        
        string ScriptFile { get; set; }

        //StatusResult Status { get; set; }
    }
}
