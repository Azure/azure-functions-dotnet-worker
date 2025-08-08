// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;

/// <summary>
/// Defines a contract for transforming <see cref="IFunctionMetadata"/> instances for Azure Functions.
/// Implementations can modify, augment, or filter function metadata before it is used by the host.
/// </summary>
public interface IFunctionMetadataTransformer
{
    /// <summary>
    /// Transforms the provided collection of <see cref="IFunctionMetadata"/> instances.
    /// </summary>
    /// <param name="original">The original collection of function metadata.</param>
    /// <returns>
    /// An <see cref="ImmutableArray{IFunctionMetadata}"/> containing the transformed function metadata.
    /// </returns>
    ImmutableArray<IFunctionMetadata> Transform(ImmutableArray<IFunctionMetadata> original);
}
