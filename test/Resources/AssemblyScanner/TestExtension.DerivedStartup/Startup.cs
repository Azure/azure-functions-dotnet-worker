// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

// The applied attribute (CustomStartupAttribute) is defined in THIS assembly (a TypeDefinition),
// but derives from the external WebJobsStartupAttribute (a TypeReference into the absent
// Microsoft.Azure.WebJobs.Host assembly). This exercises the scanner's base-type walk across the
// TypeDef -> TypeRef boundary.
using System;
using Microsoft.Azure.WebJobs.Hosting;

[assembly: TestExtension.CustomStartup(typeof(TestExtension.BazWebJobsStartup))]

namespace TestExtension
{
    public class CustomStartupAttribute : WebJobsStartupAttribute
    {
        public CustomStartupAttribute(Type startupType)
            : base(startupType)
        {
        }
    }

    public class BazWebJobsStartup
    {
    }
}
