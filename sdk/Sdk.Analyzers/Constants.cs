// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.Functions.Worker.Sdk.Analyzers
{
    internal static class Constants
    {
        internal static class Types
        {
            public const string WorkerFunctionAttribute = "Microsoft.Azure.Functions.Worker.FunctionAttribute";
            public const string WebJobsBindingAttribute = "Microsoft.Azure.WebJobs.Description.BindingAttribute";
        }

        internal static class DiagnosticsCategories
        {
            public const string Usage = "Usage";
        }
    }
}
