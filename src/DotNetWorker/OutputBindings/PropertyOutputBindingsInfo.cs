// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

﻿using System;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.Azure.Functions.Worker.OutputBindings
{
    public class PropertyOutputBindingsInfo : OutputBindingsInfo
    {
        private readonly IReadOnlyCollection<string> _propertyNames;

        public PropertyOutputBindingsInfo(IReadOnlyCollection<string> propertyNames)
        {
            _propertyNames = propertyNames ?? throw new ArgumentNullException(nameof(propertyNames));
        }

        public override IReadOnlyCollection<string> BindingNames => _propertyNames;

        public override bool BindDataToDictionary(IDictionary<string, object> dict, object? output)
        {
            if (output is not null)
            {
                Type outputType = output.GetType();

                foreach (string property in _propertyNames)
                {
                    PropertyInfo propInfo = outputType.GetProperty(property)
                        ?? throw new InvalidOperationException($"Could not find expected property '{property}' in type {outputType.FullName}");

                    var outputPropValue = propInfo.GetValue(output);

                    if (outputPropValue is not null)
                    {
                        dict[property] = outputPropValue;
                    }
                }

                return true;
            }

            return false;
        }
    }
}
