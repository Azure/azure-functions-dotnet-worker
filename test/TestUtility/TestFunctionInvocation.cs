// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Azure.Functions.Worker.Diagnostics;

namespace Microsoft.Azure.Functions.Worker.Tests
{
    public class TestFunctionInvocation : FunctionInvocation
    {
        public TestFunctionInvocation(string id = null, string functionId = null)
        {
            if (id is not null)
            {
                Id = id;
            }

            if (functionId is not null)
            {
                FunctionId = functionId;
            }

            // create/dispose activity to pull off its ID.
            using Activity activity = new Activity("Test").Start();
            Dictionary<string, string> attributes = new Dictionary<string, string>
            {
                { TraceConstants.FunctionInvocationIdKey, Guid.NewGuid().ToString() },
                { TraceConstants.AzFuncLiveLogsSessionIdKey, Guid.NewGuid().ToString() },
            };

            TraceContext = new DefaultTraceContext(activity.Id, Guid.NewGuid().ToString(), attributes);
        }

        public override string Id { get; } = Guid.NewGuid().ToString();

        public override string FunctionId { get; } = Guid.NewGuid().ToString();

        public override TraceContext TraceContext { get; }
    }
}
