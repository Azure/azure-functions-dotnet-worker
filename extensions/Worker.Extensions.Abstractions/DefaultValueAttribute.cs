// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker.Extensions.Abstractions
{
    [AttributeUsage(AttributeTargets.Property)]
    public class DefaultValueAttribute : Attribute
    {
        /// <summary>
        /// Define a default value for a property on a FunctionAttribute type.
        /// </summary>
        /// <param name="stringDefault"></param>
        public DefaultValueAttribute(string stringDefault)
        {
            DefaultStringValue = stringDefault;
        }

        public DefaultValueAttribute(bool boolDefault)
        {
            DefaultBoolValue = boolDefault;
        }

        public string? DefaultStringValue { get; }

        public bool? DefaultBoolValue { get; }
    }
}
