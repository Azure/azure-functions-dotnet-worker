// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Worker.Invocation
{
    internal class DefaultMethodInvokerFactory : IMethodInvokerFactory
    {
        public IMethodInvoker<TInstance, TReturn> Create<TInstance, TReturn>(MethodInfo method)
        {
            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }

            if (typeof(TInstance) != method.ReflectedType)
            {
                throw new InvalidOperationException("The Type must match the method's ReflectedType.");
            }

            // Parameter to invoker: TInstance instance
            ParameterExpression instanceParameter = Expression.Parameter(typeof(TInstance), "instance");

            // Parameter to invoker: object[] arguments
            ParameterExpression argumentsParameter = Expression.Parameter(typeof(object[]), "arguments");

            // Local variables passed as arguments to Call
            List<ParameterExpression> localVariables = new List<ParameterExpression>();

            // Pre-Call, copy from arguments array to local variables.
            List<Expression> arrayToLocalsAssignments = new List<Expression>();

            // Post-Call, copy from local variables back to arguments array.
            List<Expression> localsToArrayAssignments = new List<Expression>();

            // If the method returns a value: T returnValue
            ParameterExpression returnValue;

            Type returnType = method.ReturnType;
            if (returnType == typeof(void))
            {
                returnValue = null;
            }
            else
            {
                returnValue = Expression.Parameter(returnType);
            }

            ParameterInfo[] parameterInfos = method.GetParameters();
            Debug.Assert(parameterInfos != null);

            for (int index = 0; index < parameterInfos.Length; index++)
            {
                ParameterInfo parameterInfo = parameterInfos[index];
                Type argumentType = parameterInfo.ParameterType;

                if (argumentType.IsByRef)
                {
                    // The type of the local variable (and object in the arguments array) should be T rather than T&.
                    argumentType = argumentType.GetElementType()!;
                }

                // T argumentN
                ParameterExpression localVariable = Expression.Parameter(argumentType);
                localVariables.Add(localVariable);

                // arguments[index]
                Expression arrayAccess = Expression.ArrayAccess(argumentsParameter, Expression.Constant(index));

                // Pre-Call:
                // T argumentN = (T)arguments[index];
                Expression arrayAccessAsT = Expression.Convert(arrayAccess, argumentType);
                Expression assignArrayToLocal = Expression.Assign(localVariable, arrayAccessAsT);
                arrayToLocalsAssignments.Add(assignArrayToLocal);

                // Post-Call:
                // arguments[index] = (object)argumentN;
                Expression localAsObject = Expression.Convert(localVariable, typeof(object));
                Expression assignLocalToArray = Expression.Assign(arrayAccess, localAsObject);
                localsToArrayAssignments.Add(assignLocalToArray);
            }

            Expression callInstance;

            if (method.IsStatic)
            {
                callInstance = null;
            }
            else
            {
                callInstance = instanceParameter;
            }

            // Instance call:
            // instance.method(param0, param1, ...);
            // Static call:
            // method(param0, param1, ...);
            Expression call = Expression.Call(callInstance, method, localVariables);
            Expression callResult;

            if (returnType == typeof(void))
            {
                callResult = call;
            }
            else
            {
                // T returnValue = method(param0, param1, ...);
                callResult = Expression.Assign(returnValue, call);
            }

            List<Expression> blockExpressions = new List<Expression>();
            // T0 argument0 = (T0)arguments[0];
            // T1 argument1 = (T1)arguments[1];
            // ...
            blockExpressions.AddRange(arrayToLocalsAssignments);
            // Call(argument0, argument1, ...);
            // or
            // T returnValue = Call(param0, param1, ...);
            blockExpressions.Add(callResult);
            // arguments[0] = (object)argument0;
            // arguments[1] = (object)argument1;
            // ...
            blockExpressions.AddRange(localsToArrayAssignments);

            if (returnValue != null)
            {
                // return returnValue;
                blockExpressions.Add(returnValue);
            }

            List<ParameterExpression> blockVariables = new List<ParameterExpression>();
            blockVariables.AddRange(localVariables);

            if (returnValue != null)
            {
                blockVariables.Add(returnValue);
            }

            Expression block = Expression.Block(blockVariables, blockExpressions);

            if (call.Type == typeof(void))
            {
                // for: public void Function()
                var lambda = Expression.Lambda<Action<TInstance, object[]>>(
                    block,
                    instanceParameter,
                    argumentsParameter);
                Action<TInstance, object[]> compiled = lambda.Compile();
                return new VoidMethodInvoker<TInstance, TReturn>(compiled);
            }
            else if (call.Type == typeof(Task))
            {
                // for: public Task Function()
                var lambda = Expression.Lambda<Func<TInstance, object[], Task>>(
                    block,
                    instanceParameter,
                    argumentsParameter);
                Func<TInstance, object[], Task> compiled = lambda.Compile();
                return new VoidTaskMethodInvoker<TInstance, TReturn>(compiled);
            }
            else if (typeof(Task).IsAssignableFrom(call.Type))
            {
                // for: public Task<TReturn> Function()
                var lambda = Expression.Lambda<Func<TInstance, object[], Task<TReturn>>>(
                    block,
                    instanceParameter,
                    argumentsParameter);
                Func<TInstance, object[], Task<TReturn>> compiled = lambda.Compile();
                return new TaskMethodInvoker<TInstance, TReturn>(compiled);
            }
            else
            {
                // for: public TReturn Function()
                var lambda = Expression.Lambda<Func<TInstance, object[], TReturn>>(
                    block,
                    instanceParameter,
                    argumentsParameter);
                Func<TInstance, object[], TReturn> compiled = lambda.Compile();
                return new MethodInvokerWithReturnValue<TInstance, TReturn>(compiled);
            }
        }
    }
}
