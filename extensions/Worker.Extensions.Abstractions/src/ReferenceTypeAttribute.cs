// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker.Extensions.Abstractions
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class ReferenceTypeAttribute : Attribute
    {
        public bool SupportsReferenceType { get; }

        public ReferenceTypeAttribute(bool supportsReferenceType)
        {
            SupportsReferenceType = supportsReferenceType;
        }
    }
}
