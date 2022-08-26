// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading;
using Microsoft.Azure.Functions.Worker.Invocation;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests
{
    public class FunctionInvocationDictionaryTests
    {
        [Fact]
        public void TryAddInvocationDetails_ValidData_ReturnsTrue()
        {
            var invocationId = "5fb3a9b4-0b38-450a-9d46-35946e7edea7";
            var invocationDetails = new FunctionInvocationDetails()
                                    {
                                        FunctionContext = new TestFunctionContext(),
                                        CancellationTokenSource = new CancellationTokenSource(),
                                    };

            var functionInvocationDictionary = new FunctionInvocationDictionary();
            var result = functionInvocationDictionary.TryAddInvocationDetails(invocationId, invocationDetails);

            Assert.True(result);
        }

        [Fact]
        public void TryAddInvocationDetails_InvocationIdNull_ReturnsFalse()
        {
            var invocationDetails = new FunctionInvocationDetails()
                                    {
                                        FunctionContext = new TestFunctionContext(),
                                        CancellationTokenSource = new CancellationTokenSource(),
                                    };

            var functionInvocationDictionary = new FunctionInvocationDictionary();
            var result = functionInvocationDictionary.TryAddInvocationDetails(null, invocationDetails);

            Assert.False(result);
        }

        [Fact]
        public void TryAddInvocationDetails_InvocationDetailsNull_ReturnsFalse()
        {
            var invocationId = "5fb3a9b4-0b38-450a-9d46-35946e7edea7";

            var functionInvocationDictionary = new FunctionInvocationDictionary();
            var result = functionInvocationDictionary.TryAddInvocationDetails(invocationId, null);

            Assert.False(result);
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

            var functionInvocationDictionary = new FunctionInvocationDictionary();
            functionInvocationDictionary.TryAddInvocationDetails(invocationId, invocationDetails);
            functionInvocationDictionary.TryRemoveInvocationDetails(invocationId);
            functionInvocationDictionary.TryGetInvocationDetails(invocationId, out var result);

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
            var functionInvocationDictionary = new FunctionInvocationDictionary();
            functionInvocationDictionary.TryAddInvocationDetails(invocationId, invocationDetails);
            functionInvocationDictionary.TryGetInvocationDetails(invocationId, out var result);

            Assert.Equal(invocationDetails.FunctionContext, result.FunctionContext);
        }
    }
}
