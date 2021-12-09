using System.Collections.Generic;

namespace Microsoft.Azure.Functions.Worker.TestHost
{
    internal class TestFunctionMap
    {
        private readonly IDictionary<string, string> _map = new Dictionary<string, string>();

        public void AddFunction(string functionName, string functionId)
        {
            _map[functionName] = functionId;
        }

        public string GetFunctionId(string functionName)
        {
            return _map[functionName];
        }
    }
}
