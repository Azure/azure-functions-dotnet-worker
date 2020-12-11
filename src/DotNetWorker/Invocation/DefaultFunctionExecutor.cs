using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Logging;
using Microsoft.Azure.Functions.Worker.Pipeline;
using Microsoft.Azure.WebJobs.Script.Grpc.Messages;

namespace Microsoft.Azure.Functions.Worker.Invocation
{
    internal class DefaultFunctionExecutor : IFunctionExecutor
    {
        private readonly ParameterConverterManager _parameterConverterManager;
        private readonly ChannelWriter<StreamingMessage> _workerChannel;

        public DefaultFunctionExecutor(ParameterConverterManager parameterConverterManager, FunctionsHostOutputChannel outputChannel)
        {
            _parameterConverterManager = parameterConverterManager;
            _workerChannel = outputChannel.Channel.Writer;
        }

        public async Task ExecuteAsync(FunctionExecutionContext context)
        {
            Dictionary<string, object> bindingParametersDict = new Dictionary<string, object>();
            List<object?> invocationParameters = new List<object?>();
            FunctionMetadata functionMetadata = context.FunctionDefinition.Metadata;
            var pi = functionMetadata.FuncParamInfo;
            InvocationRequest invocationRequest = context.InvocationRequest;
            foreach (var param in pi)
            {
                object? paramObject;
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
            object result = await InvokeFunctionAsync(context, invocationParamArray);

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
        }

        private object? ConvertParameter(ParameterInfo param, TypedData value, ParameterConverterManager converterManager)
        {
            Type targetType = param.ParameterType;
            object? source;
            object target;

            switch (value.DataCase)
            {
                case TypedData.DataOneofCase.Http:
                    source = value.Http;
                    break;
                case TypedData.DataOneofCase.String:
                    source = value.String;
                    break;
                case TypedData.DataOneofCase.None:
                    source = null;
                    break;
                case TypedData.DataOneofCase.Json:
                    source = value.Json;
                    break;
                default:
                    throw new NotSupportedException($"{value.DataCase} is not supported yet.");
            }

            if (source is null)
            {
                return null;
            }
            else if (source.GetType() == targetType)
            {
                target = source;
            }
            else if (!converterManager.TryConvert(source, targetType, param.Name, out target))
            {
                throw new InvalidOperationException($"Unable to convert to {targetType}");
            }

            return target;
        }

        private Task<object> InvokeFunctionAsync(FunctionExecutionContext context, object[] invocationParamArray)
        {
            IFunctionInvoker? invoker = context.FunctionDefinition.Invoker;

            if (invoker == null)
            {
                throw new InvalidOperationException("Invoker cannot be null.");
            }

            object instance = invoker.CreateInstance(context.InstanceServices);
            return invoker.InvokeAsync(instance, invocationParamArray);
        }
    }
}
