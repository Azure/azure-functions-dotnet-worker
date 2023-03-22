// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Microsoft.Azure.Functions.Worker.Sdk.Generators
{
    // Trimmed version of https://github.com/dotnet/roslyn/blob/main/src/Features/Core/Portable/MakeMethodAsynchronous/AbstractMakeMethodAsynchronousCodeFixProvider.KnownTypes.cs
    internal readonly struct KnownTypes
    {
        internal readonly INamedTypeSymbol TaskType;
        internal readonly INamedTypeSymbol TaskOfTType;
        internal readonly INamedTypeSymbol ValueTaskType;
        internal readonly INamedTypeSymbol ValueTaskOfTTypeOpt;

        internal KnownTypes(Compilation compilation)
        {
            TaskType = compilation.GetTypeByMetadataName(typeof(Task).FullName)!;
            TaskOfTType = compilation.GetTypeByMetadataName(typeof(Task<>).FullName)!;
            ValueTaskType = compilation.GetTypeByMetadataName(typeof(ValueTask).FullName)!;
            ValueTaskOfTTypeOpt = compilation.GetTypeByMetadataName(typeof(ValueTask<>).FullName)!;
        }
    }
}
