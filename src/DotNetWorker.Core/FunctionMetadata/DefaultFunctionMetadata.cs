using System.Collections.Generic;

namespace Microsoft.Azure.Functions.Worker.Core.FunctionMetadata
{
    /// <summary>
    /// Local representation of FunctionMetadata
    /// </summary>
    public class DefaultFunctionMetadata : IFunctionMetadata
    {
        /// <inheritdoc/>
        public string? FunctionId { get; set; }

        /// <inheritdoc/>
        public bool IsProxy { get; set; }

        /// <inheritdoc/>
        public string? Language { get; set; }

        /// <inheritdoc/>
        public bool ManagedDependencyEnabled { get; set; }

        /// <inheritdoc/>
        public string? Name { get; set; }

        /// <inheritdoc/>
        public string? EntryPoint { get; set; }

        /// <inheritdoc/>
        public IList<string>? RawBindings { get; set; }

        /// <inheritdoc/>
        public string? ScriptFile { get; set; }
    }
}
