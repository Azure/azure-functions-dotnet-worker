// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;

namespace Microsoft.Azure.Functions.Worker.Sdk.Generators.Extensions
{
    internal static class AttributeDataExtensions
    {
        /// <summary>
        /// Determines whether an attribute is a Function Binding or not. This method assumes that any Function Binding related attributes passed in are
        /// of the highest level (specific bindings like QueueTrigger, Queue Input Binding, etc).
        /// </summary>
        /// <param name="attribute">The exact attribute decorating a method/parameter/property declaration.</param>
        internal static bool IsBindingAttribute(this AttributeData attribute)
        {
            if (attribute.AttributeClass?.BaseType?.BaseType is not null)
            {
                return String.Equals(attribute.AttributeClass.BaseType.BaseType.GetFullName(), Constants.BindingAttributeType);
            }

            return false;
        }

        internal static bool IsOutputBindingAttribute(this AttributeData attribute)
        {
            if (attribute.AttributeClass?.BaseType != null)
            {
                return String.Equals(attribute.AttributeClass.BaseType.GetFullName(), Constants.OutputBindingAttributeType);
            }

            return false;
        }

        internal static bool IsHttpTrigger(this AttributeData attribute)
        {
            if (attribute.AttributeClass != null)
            {
                return String.Equals(attribute.AttributeClass.GetFullName(), Constants.HttpTriggerBindingType);
            }

            return false;
        }

        internal static bool IsEventHubsTrigger(this AttributeData attribute)
        {
            if (attribute.AttributeClass != null)
            {
                return String.Equals(attribute.AttributeClass.GetFullName(), Constants.EventHubsTriggerType);
            }

            return false;
        }


    }
}
