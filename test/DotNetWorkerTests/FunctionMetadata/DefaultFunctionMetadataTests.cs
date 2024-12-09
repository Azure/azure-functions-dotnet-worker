// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests.FunctionMetadata
{
    public class DefaultFunctionMetadataTests
    {
        [Fact]
        public void FunctionId_StableHash_SameValues()
        {
            DefaultFunctionMetadata func1 = new()
            {
                Name = Guid.NewGuid().ToString(),
                EntryPoint = Guid.NewGuid().ToString(),
                ScriptFile = Guid.NewGuid().ToString(),
            };

            DefaultFunctionMetadata func2 = new()
            {
                Name = func1.Name,
                EntryPoint = func1.EntryPoint,
                ScriptFile = func1.ScriptFile,
            };

            string func1Id = func1.FunctionId;
            Assert.NotNull(func1.FunctionId);
            Assert.Equal(func1Id, func1.FunctionId); // does not change
            Assert.Equal(func1.FunctionId, func2.FunctionId);
        }

        [Fact]
        public void FunctionId_StableHash_DifferentValues()
        {
            DefaultFunctionMetadata func1 = new()
            {
                Name = Guid.NewGuid().ToString(),
                EntryPoint = Guid.NewGuid().ToString(),
                ScriptFile = Guid.NewGuid().ToString(),
            };

            DefaultFunctionMetadata func2 = new()
            {
                Name = func1.Name + "2",
                EntryPoint = func1.EntryPoint,
                ScriptFile = func1.ScriptFile,
            };

            Assert.NotEqual(func1.FunctionId, func2.FunctionId);
        }
    }
}
