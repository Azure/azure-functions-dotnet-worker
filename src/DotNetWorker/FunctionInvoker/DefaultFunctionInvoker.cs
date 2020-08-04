using Microsoft.Azure.Functions.DotNetWorker.Converters;
using Microsoft.Azure.Functions.DotNetWorker.Logging;
using Microsoft.Azure.Functions.DotNetWorker.Pipeline;
using Microsoft.Azure.WebJobs.Script.Grpc.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.DotNetWorker.FunctionInvoker
{
    internal class DefaultFunctionInvoker : IFunctionInvoker
    {
        ParameterConverterManager _parameterConverterManager;
        ChannelWriter<StreamingMessage> _workerChannel;
        private IFunctionInstanceFactory _functionInstanceFactory;

        public DefaultFunctionInvoker(ParameterConverterManager parameterConverterManager, FunctionsHostOutputChannel outputChannel, IFunctionInstanceFactory functionInstanceFactory)
        {
            _parameterConverterManager = parameterConverterManager;
            _workerChannel = outputChannel.Channel.Writer;
            _functionInstanceFactory = functionInstanceFactory;
        }

        public Task InvokeAsync(FunctionExecutionContext context)
        {
            Dictionary<string, object> bindingParametersDict = new Dictionary<string, object>();
            List<object> invocationParameters = new List<object>();
            FunctionDescriptor functionDescriptor = context.FunctionDescriptor;
            var pi = functionDescriptor.FuncParamInfo;
            InvocationRequest invocationRequest = context.InvocationRequest;
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
                        paramObject = ConvertParameter(param, value, _parameterConverterManager);
                    }
                }
                else if (parameterType == typeof(FunctionExecutionContext))
                {
                    context.Logger = new InvocationLogger(context.InvocationRequest.InvocationId, _workerChannel);
                    paramObject = context;
                }
                else
                {
                    TypedData value = invocationRequest.InputData.Where(p => p.Name == param.Name).First().Data;
                    paramObject = ConvertParameter(param, value, _parameterConverterManager);
                }
                invocationParameters.Add(paramObject);
            }

            var invocationParamArray = invocationParameters.ToArray();
            object result = InvokeFunction(functionDescriptor, invocationParamArray, _functionInstanceFactory);

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
