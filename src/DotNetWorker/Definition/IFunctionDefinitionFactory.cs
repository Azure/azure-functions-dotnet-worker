// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Script.Grpc.Messages;

namespace Microsoft.Azure.Functions.Worker
{
    public interface IFunctionDefinitionFactory
    {
        FunctionDefinition Create(FunctionLoadRequest request);
    }
}
