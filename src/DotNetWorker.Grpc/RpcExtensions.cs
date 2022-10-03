// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Azure.Core.Serialization;
using Google.Protobuf;
using Microsoft.Azure.Functions.Worker.Definition;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Invocation;

namespace Microsoft.Azure.Functions.Worker.Rpc
{
    internal static class RpcExtensions
    {
        private static readonly TypedData _emptyTypedData = new();
        private static readonly Task<TypedData> _emptyTypedDataResult = Task.FromResult(_emptyTypedData);

        public static Task<TypedData> ToRpcAsync(this object value, ObjectSerializer serializer)
        {
            if (value == null)
            {
                return _emptyTypedDataResult;
            }

            var typedData = new TypedData();
            if (value is byte[] arr)
            {
                typedData.Bytes = ByteString.CopyFrom(arr);
            }
            else if (value is HttpResponseData response)
            {
                return response.ToRpcHttpAsync(serializer).ContinueWith(t =>
                {
                    typedData.Http = t.Result;
                    return typedData;
                });
            }
            else if (value is string str)
            {
                typedData.String = str;
            }
            else
            {
                typedData = value.ToRpcDefault(serializer);
            }

            return Task.FromResult(typedData);
        }

        public static TypedData ToRpc(this object value, ObjectSerializer serializer) =>
            value switch
            {
                null => _emptyTypedData,
                byte[] arr => new TypedData() { Bytes = ByteString.CopyFrom(arr) },
                string str => new TypedData() { String = str },
                double dbl => new TypedData() { Double = dbl },
                _ => value.ToRpcDefault(serializer)
            };

        internal static TypedData ToRpcDefault(this object value, ObjectSerializer serializer)
        {
            // attempt POCO / array of pocos
            TypedData typedData = new TypedData();
            try
            {
                typedData.Json = serializer.Serialize(value)?.ToString();
            }
            catch
            {
                typedData.String = value.ToString();
            }

            return typedData;
        }

        internal static Task<RpcHttp> ToRpcHttpAsync(this HttpResponseData response, ObjectSerializer serializer)
        {
            if (response is GrpcHttpResponseData rpcResponse)
            {
                return rpcResponse.GetRpcHttpAsync();
            }

            // TODO: Review non-RPC response implementations
            // which will be handled by the code below
            var http = new RpcHttp()
            {
                StatusCode = ((int)response.StatusCode).ToString()
            };

            if (response.Body != null)
            {
                http.Body = response.Body.ToRpc(serializer);
            }
            else
            {
                // TODO: Is this correct? Passing a null body causes the entire
                //       response to become the body in functions. Need to investigate.
                http.Body = string.Empty.ToRpc(serializer);
            }

            if (response.Headers != null)
            {
                foreach (var pair in response.Headers)
                {
                    // maybe check or validate that the headers make sense?
                    http.Headers.Add(pair.Key.ToLowerInvariant(), pair.Value.ToString());
                }
            }

            return Task.FromResult(http);
        }

        internal static FunctionDefinition ToFunctionDefinition(this FunctionLoadRequest loadRequest, IMethodInfoLocator methodInfoLocator)
        {
            return new GrpcFunctionDefinition(loadRequest, methodInfoLocator);
        }

        /// <summary>
        /// Returns an RpcException for system exception scenarios.
        /// </summary>
        /// <returns>An RpcException</returns>
        internal static RpcException? ToRpcException(this Exception exception)
        {
            if (exception is null)
            {
                return null;
            }

            return new RpcException
            {
                Message = exception.ToString(),
                Source = exception.Source ?? string.Empty,
                StackTrace = exception.StackTrace ?? string.Empty
            };
        }

        /// <summary>
        /// Returns an RpcException for when an exception is thrown by user's code. 
        /// </summary>
        /// <returns>An RpcException with IsUserException set to true.</returns>
        internal static RpcException? ToUserRpcException(this Exception exception)
        {
            if (exception is null)
            {
                return null;
            }

            return new RpcException
            {
                Message = exception.Message,
                Source = exception.Source ?? string.Empty,
                StackTrace = exception.StackTrace ?? string.Empty,
                Type = exception.GetType().FullName ?? string.Empty,
                IsUserException = true
            };
        }
    }
}
