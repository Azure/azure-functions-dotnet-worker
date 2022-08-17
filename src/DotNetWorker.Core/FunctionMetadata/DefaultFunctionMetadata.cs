using System.Collections.Generic;

namespace Microsoft.Azure.Functions.Worker.Core.FunctionMetadata
{
    /// <summary>
    /// Local representation of FunctionMetadata
    /// </summary>
    public class DefaultFunctionMetadata : IFunctionMetadata
    {
        /// <summary>
        /// Creates a DefaultFunctionMetadata with the minimum required properties for successful function loading.
        /// </summary>
        /// <param name="functionId">Unique ID associated with each function.</param>
        /// <param name="language">The language that the function is written in.</param>
        /// <param name="name">The function name</param>
        /// <param name="entryPoint">The function entrypoint</param>
        /// <param name="rawBindings">List of the functions bindings in JSON string format</param>
        /// <param name="scriptFile">The path to the function app assembly</param>
        public DefaultFunctionMetadata(string functionId, string language, string name, string entryPoint, IList<string> rawBindings, string scriptFile)
        {
            _functionId = functionId;
            _language = language;
            _name = name;
            _entryPoint = entryPoint;
            _rawBindings = rawBindings;
            _scriptFile = scriptFile;
        }

        private string _functionId;
        /// <inheritdoc/>
        public string FunctionId { get => _functionId; set => _functionId = value; }

        private bool _isProxy;
        /// <inheritdoc/>
        public bool IsProxy { get => _isProxy; set => _isProxy = value; }

        private string _language;
        /// <inheritdoc/>
        public string Language { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

        private bool managedDependencyEnabled;
        /// <inheritdoc/>
        public bool ManagedDependencyEnabled { get => managedDependencyEnabled; set => managedDependencyEnabled = value; }

        private string _name;
        /// <inheritdoc/>
        public string Name { get => _name; set => _name = value; }

        private string _entryPoint;
        /// <inheritdoc/>
        public string EntryPoint { get => _entryPoint; set => _entryPoint = value; }

        private IList<string> _rawBindings;
        /// <inheritdoc/>
        public IList<string> RawBindings => _rawBindings;

        private string _scriptFile;
        /// <inheritdoc/>
        public string ScriptFile { get => _scriptFile; set => _scriptFile = value; }
    }
}
