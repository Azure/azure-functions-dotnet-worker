using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.Azure.Functions.Worker.Sdk
{
    // Copy of FunctionMetadata, but using internal type to simplify dependencies.
    internal class SdkFunctionMetadata
    {
        public string Name { get; set; }

        public string ScriptFile { get; set; }

        public string FunctionDirectory { get; set; }

        public string EntryPoint { get; set; }

        public string Language { get; set; }

        public IDictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

        public Collection<object> Bindings { get; set; } = new Collection<object>();
    }
}
