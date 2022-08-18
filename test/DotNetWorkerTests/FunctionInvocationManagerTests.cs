// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading;
using Microsoft.Azure.Functions.Worker.Invocation;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests
{
    public class FunctionInvocationManagerTests
    {
        [Fact]
        public void TryAddInvocationDetails_ValidData_AddsInfoToInflightDict()
        {
            var invocationId = "5fb3a9b4-0b38-450a-9d46-35946e7edea7";
            var invocationDetails = new FunctionInvocationDetails()
                                    {
                                        FunctionContext = new TestFunctionContext(),
                                        CancellationTokenSource = new CancellationTokenSource(),
                                    };

            var functionInvocationManager = new FunctionInvocationManager();
            functionInvocationManager.TryAddInvocationDetails(invocationId, invocationDetails);

            functionInvocationManager._inflightInvocations.TryGetValue(invocationId, out var result);
            Assert.Equal(invocationDetails, result);
        }

        [Fact]
        public void TryAddInvocationDetails_InvocationIdNull_NoAction()
        {
            var invocationDetails = new FunctionInvocationDetails()
                                    {
                                        FunctionContext = new TestFunctionContext(),
                                        CancellationTokenSource = new CancellationTokenSource(),
                                    };

            var functionInvocationManager = new FunctionInvocationManager();
            functionInvocationManager.TryAddInvocationDetails(null, invocationDetails);

            Assert.Empty(functionInvocationManager._inflightInvocations);
        }

        [Fact]
        public void TryAddInvocationDetails_InvocationDetailsNull_InflightDictNotUpdated()
        {
            var invocationId = "5fb3a9b4-0b38-450a-9d46-35946e7edea7";

            var functionInvocationManager = new FunctionInvocationManager();
            functionInvocationManager.TryAddInvocationDetails(invocationId, null);

            functionInvocationManager._inflightInvocations.TryGetValue(invocationId, out var result);
            Assert.Null(result);
        }

        [Fact]
        public void TryRemoveInvocationDetails_InvocationIdExists_RemovesFromInflightDict()
        {
            var invocationId = "5fb3a9b4-0b38-450a-9d46-35946e7edea7";
            var invocationDetails = new FunctionInvocationDetails()
                                    {
                                        FunctionContext = new TestFunctionContext(),
                                        CancellationTokenSource = new CancellationTokenSource(),
                                    };

            var functionInvocationManager = new FunctionInvocationManager();
            functionInvocationManager._inflightInvocations.TryAdd(invocationId, invocationDetails);

            functionInvocationManager.TryRemoveInvocationDetails(invocationId);

            functionInvocationManager._inflightInvocations.TryGetValue(invocationId, out var result);
            Assert.Null(result);
        }

        [Fact]
        public void TryGetInvocationDetails_InvocationIdAndDetailsExist_ReturnsFunctionInvocationDetails()
        {
            var invocationId = "5fb3a9b4-0b38-450a-9d46-35946e7edea7";
            var invocationDetails = new FunctionInvocationDetails()
                                    {
                                        FunctionContext = new TestFunctionContext(),
                                        CancellationTokenSource = new CancellationTokenSource(),
                                    };
            var functionInvocationManager = new FunctionInvocationManager();
            functionInvocationManager._inflightInvocations.TryAdd(invocationId, invocationDetails);

            var result = functionInvocationManager.TryGetInvocationDetails(invocationId);

            Assert.Equal(invocationDetails.FunctionContext, result.FunctionContext);
        }

        [Fact]
        public void TryGetInvocationDetails_FunctionInvocationDetailsNull_ReturnsNull()
        {
            var invocationId = "5fb3a9b4-0b38-450a-9d46-35946e7edea7";
            var functionInvocationManager = new FunctionInvocationManager();
            functionInvocationManager._inflightInvocations.TryAdd(invocationId, null);

            var result = functionInvocationManager.TryGetInvocationDetails(invocationId);

            Assert.Null(result);
        }
    }
}
