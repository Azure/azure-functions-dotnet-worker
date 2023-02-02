using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

#nullable disable
namespace Microsoft.Azure.Functions.Worker.Sdk.Generators
{

    // Trimmed version of https://github.com/dotnet/roslyn/blob/main/src/Features/Core/Portable/MakeMethodAsynchronous/AbstractMakeMethodAsynchronousCodeFixProvider.KnownTypes.cs

    internal readonly struct KnownTypes
    {
        public readonly INamedTypeSymbol TaskType;
        public readonly INamedTypeSymbol TaskOfTType;
        public readonly INamedTypeSymbol ValueTaskType;
        public readonly INamedTypeSymbol ValueTaskOfTTypeOpt;

        internal KnownTypes(Compilation compilation)
        {
            TaskType = compilation.GetTypeByMetadataName(typeof(Task).FullName!);
            TaskOfTType = compilation.GetTypeByMetadataName(typeof(Task<>).FullName!);
            ValueTaskType = compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask");
            ValueTaskOfTTypeOpt = compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1");
        }
    }
}
