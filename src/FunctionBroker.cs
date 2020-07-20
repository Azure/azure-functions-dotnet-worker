using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using FunctionsDotNetWorker.Converters;
using FunctionsDotNetWorker.Logging;
using Microsoft.Azure.WebJobs.Script.Grpc.Messages;

namespace FunctionsDotNetWorker
{
    public class FunctionBroker : IFunctionBroker
    {
        private readonly ParameterConverterManager _converterManager;
        private Dictionary<string, FunctionDescriptor> _functionMap = new Dictionary<string, FunctionDescriptor>();

        public FunctionBroker(ParameterConverterManager converterManager)
        {
            _converterManager = converterManager;
        }

        public void AddFunction(FunctionLoadRequest functionLoadRequest)
        {
            FunctionDescriptor functionDescriptor = new FunctionDescriptor(functionLoadRequest);

            _functionMap.Add(functionDescriptor.FunctionID, functionDescriptor);
        }

        public object Invoke(InvocationRequest invocationRequest, out List<ParameterBinding> parameterBindings, WorkerLogManager workerLogManager)
        {
            parameterBindings = new List<ParameterBinding>();
            Dictionary<string, object> bindingParametersDict = new Dictionary<string, object>();
            List<object> invocationParameters = new List<object>();
            FunctionDescriptor functionDescriptor = _functionMap[invocationRequest.FunctionId];
            var pi = functionDescriptor.FuncParamInfo;

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
                        Type constructed = genericType.MakeGenericType(new Type[] {elementType});
                        paramObject = Activator.CreateInstance(constructed);
                        bindingParametersDict.Add(param.Name, paramObject);
                    }
                    else
                    {
                        TypedData value = invocationRequest.InputData.Where(p => p.Name == param.Name).First().Data;
                        paramObject = getParamObject(param, value);
                    }
                }
                else if (parameterType == typeof(FunctionExecutionContext))
                {
                    paramObject = new FunctionExecutionContext(invocationRequest, functionDescriptor.FuncName, workerLogManager);
                }
                else
                {
                    TypedData value = invocationRequest.InputData.Where(p => p.Name == param.Name).First().Data;
                    paramObject = getParamObject(param, value);
                }
                invocationParameters.Add(paramObject);
            }

            var invocationParamArray = invocationParameters.ToArray();
            object result = invokeFunction(functionDescriptor, invocationParamArray);

            foreach(var binding in bindingParametersDict)
            {
                var rpcVal = OutBindingConverter.ExtractValue(binding.Value);
                var parameterBinding = new ParameterBinding
                {
                    Name = binding.Key,
                    Data = rpcVal.ToRpc() 
                };
                parameterBindings.Add(parameterBinding);
            }

            return result;
        }

        private object invokeFunction(FunctionDescriptor functionDescriptor, object[] invocationParamArray)
        {
            MethodInfo mi = functionDescriptor.FuncMethodInfo;  
            if (mi.IsStatic)
            {
                return mi.Invoke(null, invocationParamArray);
            }
            else
            {
                object instanceObject = Activator.CreateInstance(functionDescriptor.FunctionType);
                return mi.Invoke(instanceObject, invocationParamArray);
            }
        }

        private object getParamObject(ParameterInfo param, TypedData value)
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
            else if (!_converterManager.TryConvert(source, targetType, param.Name, out target))
            {
                throw new InvalidOperationException($"Unable to convert to {targetType}");
            }

            return target; 
        }
    }

}
