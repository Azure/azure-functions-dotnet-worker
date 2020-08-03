using Microsoft.Azure.Functions.DotNetWorker.Converters;
using Microsoft.Azure.WebJobs.Script.Grpc.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.DotNetWorker.FunctionInvoker
{
    internal class DefaultFunctionInvoker : IFunctionInvoker
    {
        public Task InvokeAsync(FunctionExecutionContext context)
        {
            Dictionary<string, object> bindingParametersDict = new Dictionary<string, object>();
            List<object> invocationParameters = new List<object>();
            FunctionDescriptor functionDescriptor = context.FunctionDescriptor;
            var pi = functionDescriptor.FuncParamInfo;
            InvocationRequest invocationRequest = context.InvocationRequest;
            ChannelWriter<StreamingMessage> _workerChannel = context.ChannelWriter;

            foreach (var param in pi)
            {
                object paramObject;
                Type parameterType = param.ParameterType;
                if (parameterType.IsGenericType)
                {
                    var genericType = parameterType.GetGenericTypeDefinition();
                    var elementType = parameterType.GetGenericArguments()[0];
                    if (genericType == typeof(OutputBinding<>))
                    {
                        Type constructed = genericType.MakeGenericType(new Type[] { elementType });
                        paramObject = Activator.CreateInstance(constructed);
                        bindingParametersDict.Add(param.Name, paramObject);
                    }
                    else
                    {
                        TypedData value = invocationRequest.InputData.Where(p => p.Name == param.Name).First().Data;
                        paramObject = ConvertParameter(param, value, context.ParameterConverterManager);
                    }
                }
                else if (parameterType == typeof(FunctionExecutionContext))
                {
                    paramObject = context;
                }
                else
                {
                    TypedData value = invocationRequest.InputData.Where(p => p.Name == param.Name).First().Data;
                    paramObject = ConvertParameter(param, value, context.ParameterConverterManager);
                }
                invocationParameters.Add(paramObject);
            }

            var invocationParamArray = invocationParameters.ToArray();
            object result = InvokeFunction(functionDescriptor, invocationParamArray, context.FunctionInstanceFactory);

            foreach (var binding in bindingParametersDict)
            {
                dynamic d = binding.Value;
                var rpcVal = d.GetValue();
                var parameterBinding = new ParameterBinding
                {
                    Name = binding.Key,
                    Data = RpcExtensions.ToRpc(rpcVal)
                };
                context.ParameterBindings.Add(parameterBinding);
            }

            context.InvocationResult = result;
            return Task.CompletedTask;
        }

        private object ConvertParameter(ParameterInfo param, TypedData value, ParameterConverterManager converterManager)
        {
            Type targetType = param.ParameterType;
            object source;
            object target;

            switch (value.DataCase)
            {
                case TypedData.DataOneofCase.Http:
                    source = value.Http;
                    break;
                case TypedData.DataOneofCase.String:
                    source = value.String;
                    break;
                default:
                    throw new NotSupportedException($"{value.DataCase} is not supported yet.");
            }

            if (source.GetType() == targetType)
            {
                target = source;
            }
            else if (!converterManager.TryConvert(source, targetType, param.Name, out target))
            {
                throw new InvalidOperationException($"Unable to convert to {targetType}");
            }

            return target;
        }

        private object InvokeFunction(FunctionDescriptor functionDescriptor, object[] invocationParamArray, IFunctionInstanceFactory functionInstanceFactory)
        {
            MethodInfo mi = functionDescriptor.FuncMethodInfo;
            if (mi.IsStatic)
            {
                return mi.Invoke(null, invocationParamArray);
            }
            else
            {
                object instanceObject = functionInstanceFactory.CreateInstance(functionDescriptor.FunctionType);
                return mi.Invoke(instanceObject, invocationParamArray);
            }
        }
    }
}
