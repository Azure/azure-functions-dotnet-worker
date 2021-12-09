using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.Azure.Functions.Worker.Invocation;
using Microsoft.Azure.WebJobs.Script.Description;

namespace Microsoft.Azure.Functions.Worker.TestHost
{
    internal class TestFunctionDefinition : FunctionDefinition
    {
        private readonly FunctionMetadata _metadata;

        public TestFunctionDefinition(FunctionMetadata metadata, IMethodInfoLocator methodInfoLocator)
        {
            _metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));

            PathToAssembly = Path.GetFullPath(_metadata.ScriptFile);

            InputBindings = _metadata.InputBindings.ToImmutableDictionary(b => b.Name, b => (BindingMetadata)new TestBindingMetadata(b));
            OutputBindings = _metadata.OutputBindings.ToImmutableDictionary(b => b.Name, b => (BindingMetadata)new TestBindingMetadata(b));

            Parameters = methodInfoLocator.GetMethod(PathToAssembly, EntryPoint)
               .GetParameters()
               .Where(p => p.Name != null)
               .Select(p => new FunctionParameter(p.Name!, p.ParameterType))
               .ToImmutableArray();
        }

        public override ImmutableArray<FunctionParameter> Parameters { get; }

        public override string PathToAssembly { get; }

        public override string EntryPoint => _metadata.EntryPoint;

        public override string Id { get; } = Guid.NewGuid().ToString();

        public override string Name => _metadata.Name;

        public override IImmutableDictionary<string, BindingMetadata> InputBindings { get; }

        public override IImmutableDictionary<string, BindingMetadata> OutputBindings { get; }
    }
}
