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
            FunctionId = functionId;
            Language = language;
            Name = name;
            EntryPoint = entryPoint;
            RawBindings = rawBindings;
            ScriptFile = scriptFile;
        }

        /// <summary>
        /// Unique ID associated with each function.
        /// </summary>
        public string FunctionId { get; set; }

        /// <summary>
        /// Notes whether a function is a proxy function (true) or not (false).
        /// </summary>
        public bool IsProxy { get; set; }

        /// <summary>
        /// The language that the function is written in.
        /// </summary>
        public string Language { get; set; }

        /// <summary>
        /// Notes whether managed depenedcy is enabled (true) or not (false).
        /// </summary>
        public bool ManagedDependencyEnabled { get; set; }

        /// <summary>
        /// The name of the function.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The entry point of the function.
        /// </summary>
        public string EntryPoint { get; set; }

        /// <summary>
        /// The bindings in this function represented as a list of json strings.
        /// </summary>
        public IList<string> RawBindings { get; }

        /// <summary>
        /// The path to the function app assembly.
        /// </summary>
        public string ScriptFile { get; set; }
    }
}
