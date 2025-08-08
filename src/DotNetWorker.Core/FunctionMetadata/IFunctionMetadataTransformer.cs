// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;

/// <summary>
/// Defines a method to transform <see cref="IFunctionMetadata"/> for Azure Functions.
/// </summary>
// Not yet sold on the naming - could also be an "Augmenter" or "Enhancer"
public interface IFunctionMetadataTransformer
{
    /// <summary>
    /// Transforms the specified <see cref="IFunctionMetadata"/> using the provided <see cref="FunctionContext"/>.
    /// </summary>
    /// <param name="original">The original function metadata.</param>
    /// <returns>The transformed function metadata.</returns>
    IFunctionMetadata Transform(IFunctionMetadata original);
}
