// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker.Extensions.Abstractions
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class ExtensionInformationAttribute : Attribute
    {
        public string ExtensionPackage { get; }

        public string ExtensionVersion { get; }

        public bool EnableImplicitRegistration { get; }

        public ExtensionInformationAttribute(string extensionPackage, string extensionVersion)
            : this(extensionPackage, extensionVersion, false)
        {
        }

        public ExtensionInformationAttribute(string extensionPackage, string extensionVersion, bool enableImplicitRegistration)
        {
            ExtensionPackage = extensionPackage;
            ExtensionVersion = extensionVersion;
            EnableImplicitRegistration = enableImplicitRegistration;
        }
    }
}
