// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;

namespace Microsoft.Azure.Functions.Worker.Sdk.Analyzers;

internal static class AttributeDataExtensions
{
    /// <summary>
    /// Checks if an attribute is a web jobs attribute.
    /// </summary>
    /// <param name="attributeData">The attribute to check.</param>
    /// <returns>A boolean value indicating whether the attribute is a web jobs attribute.</returns>
    public static bool IsWebJobAttribute(this AttributeData attributeData)
    {
        // Gets the attributes applied on the Attribute class we are checking.
        var attributeAttributes = attributeData.AttributeClass?.GetAttributes();

        if (attributeAttributes is null)
        {
            return false;
        }

        foreach (var attribute in attributeAttributes)
        {
            if (string.Equals(attribute.AttributeClass?.ToDisplayString(), Constants.Types.WebJobsBindingAttribute,
                    StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if an attribute is the SupportsDeferredBinding attribute.
    /// </summary>
    /// <param name="attributeData">The attribute to check.</param>
    /// <returns>A boolean value indicating whether the attribute is a SupportsDeferredBinding attribute.</returns>
    public static bool IsSupportsDeferredBindingAttribute(this AttributeData attributeData)
    {
        if (string.Equals(attributeData.AttributeClass?.ToDisplayString(),
                            Constants.Types.SupportsDeferredBindingAttribute,
                            StringComparison.Ordinal))
        {
            return true;
        }

        return false;
    }
}
