// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.WebJobs.Hosting
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class WebJobsStartupAttribute : Attribute
    {
        public WebJobsStartupAttribute(Type startupType)
        {
        }

        public WebJobsStartupAttribute(Type startupType, string name)
        {
        }
    }
}

namespace Microsoft.Azure.Functions.Worker.Extensions.Abstractions
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public sealed class ExtensionInformationAttribute : Attribute
    {
        public ExtensionInformationAttribute(string extensionName, string extensionVersion)
        {
        }
    }
}
