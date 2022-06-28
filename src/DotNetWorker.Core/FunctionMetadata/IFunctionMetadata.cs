using System.Collections.Generic;

namespace Microsoft.Azure.Functions.Worker.Core.FunctionMetadata
{
    /// <summary>
    /// Public interface that represents properties in function metadata.
    /// </summary>
    public interface IFunctionMetadata
    {
        /// <summary>
        /// Unique function id.
        /// </summary>
        string FunctionId { get; set; }

        /// <summary>
        /// If function is a proxy function return true, else return false.
        /// </summary>
        bool IsProxy { get; set; }

        /// <summary>
        /// Language that the function is written in.
        /// </summary>
        string Language { get; set; }

        /// <summary>
        /// If managed dependency is enabled return true, else return false.
        /// </summary>
        bool ManagedDependencyEnabled { get; set; }

        /// <summary>
        /// Name of function.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Function entrypoint (ex. HttpTrigger.Run).
        /// </summary>
        string EntryPoint { get; set; }

        /// <summary>
        /// List of function's bindings in json string format.
        /// </summary>
        IList<string> RawBindings { get; }
        
        /// <summary>
        /// The function app assembly (ex. FunctionApp.dll).
        /// </summary>
        string ScriptFile { get; set; }
    }
}
