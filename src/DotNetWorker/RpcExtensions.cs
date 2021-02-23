// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Azure.Core.Serialization;
using System.Threading.Tasks;
using Google.Protobuf;
using Microsoft.Azure.Functions.Worker.Definition;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Script.Grpc.Messages;

namespace Microsoft.Azure.Functions.Worker
{
    internal static class RpcExtensions
    {
        private static TypedData _emptyTypedData = new();
        private static Task<TypedData> _emptyTypedDataResult = Task.FromResult(_emptyTypedData);

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
            else if (value.GetType().IsArray)
            {
                typedData = ToRpcCollection(value, serializer);
            }
            else
            {
                typedData = value.ToRpcDefault(serializer);
            }

            return Task.FromResult(typedData);
        }

        public static TypedData ToRpc(this object value, ObjectSerializer serializer)
        {
             if (value == null)
            {
                return _emptyTypedData;
            }

            var typedData = new TypedData();

            if (value is byte[] arr)
            {
                typedData.Bytes = ByteString.CopyFrom(arr);
            }
            else if (value is string str)
            {
                typedData.String = str;
            }
            else if (value.GetType().IsArray)
            {
                typedData = ToRpcCollection(value, serializer);
            }
            else
            {
                typedData = value.ToRpcDefault(serializer);
            }

            return typedData;
        }

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

        public static TypedData ToRpcCollection(this object value, ObjectSerializer serializer)
        {
            TypedData typedData;
            if (value is byte[][] arrBytes)
            {
                typedData = arrBytes.ToRpcByteArray();
            }
            else if (value is string[] arrStr)
            {
                typedData = arrStr.ToRpcStringArray();
            }
            else if (value is double[] arrDouble)
            {
                typedData = arrDouble.ToRpcDoubleArray();
            }
            else if (value is long[] arrLong)
            {
                typedData = arrLong.ToRpcLongArray();
            }
            else
            {
                typedData = value.ToRpcDefault(serializer);
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

        internal static TypedData ToRpcByteArray(this byte[][] arrBytes)
        {
            TypedData typedData = new TypedData();
            CollectionBytes collectionBytes = new CollectionBytes();
            foreach (byte[] element in arrBytes)
            {
                if (element != null)
                {
                    collectionBytes.Bytes.Add(ByteString.CopyFrom(element));
                }
            }
            typedData.CollectionBytes = collectionBytes;

            return typedData;
        }

        internal static TypedData ToRpcStringArray(this string[] arrString)
        {
            TypedData typedData = new TypedData();
            CollectionString collectionString = new CollectionString();
            foreach (string element in arrString)
            {
                if (!string.IsNullOrEmpty(element))
                {
                    collectionString.String.Add(element);
                }
            }
            typedData.CollectionString = collectionString;

            return typedData;
        }

        internal static TypedData ToRpcDoubleArray(this double[] arrDouble)
        {
            TypedData typedData = new TypedData();
            CollectionDouble collectionDouble = new CollectionDouble();
            foreach (double element in arrDouble)
            {
                collectionDouble.Double.Add(element);
            }
            typedData.CollectionDouble = collectionDouble;

            return typedData;
        }

        internal static TypedData ToRpcLongArray(this long[] arrLong)
        {
            TypedData typedData = new TypedData();
            CollectionSInt64 collectionLong = new CollectionSInt64();
            foreach (long element in arrLong)
            {
                collectionLong.Sint64.Add(element);
            }
            typedData.CollectionSint64 = collectionLong;

            return typedData;
        }

        internal static FunctionMetadata ToFunctionMetadata(this FunctionLoadRequest loadRequest) => new GrpcFunctionMetadata(loadRequest);

        internal static RpcException? ToRpcException(this Exception exception)
        {
            if (exception == null)
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
    }
}
