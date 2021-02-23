// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

﻿using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Azure.Functions.Worker.Http;

namespace Microsoft.Azure.Functions.Worker.OutputBindings
{
    internal class PropertyOutputBindingsInfo : OutputBindingsInfo
    {
        private readonly IReadOnlyCollection<string> _propertyNames;

        public PropertyOutputBindingsInfo(IReadOnlyCollection<string> propertyNames)
        {
            _propertyNames = propertyNames ?? throw new ArgumentNullException(nameof(propertyNames));
        }

        public override void BindOutputInContext(FunctionContext context)
        {
            object? result = context.InvocationResult;

            if (result is not null)
            {
                AddResultToOutputBindings(context.OutputBindings, result);

                // Because this context had property output bindings,
                // any invocation result was tranformed to output bindings, so the invocation result
                // would now be null.
                context.InvocationResult = null;
            }
        }

        private void AddResultToOutputBindings(IDictionary<string, object> outputBindings, object result)
        {
            Type resultType = result.GetType();

            foreach (string property in _propertyNames)
            {
                var propValue = GetPropertyValue(resultType, property, result);

                if (propValue is not null)
                {
                    outputBindings[property] = propValue;
                }
            }
        }

        private static object? GetPropertyValue(Type baseType, string propertyName, object instance)
        {
            PropertyInfo propInfo = baseType.GetProperty(propertyName)
                        ?? throw new InvalidOperationException($"Could not find expected property '{propertyName}' in type {baseType.FullName}");

            return propInfo.GetValue(instance);
        }
    }
}
