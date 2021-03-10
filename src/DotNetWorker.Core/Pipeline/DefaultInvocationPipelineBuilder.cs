// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Middleware;

namespace Microsoft.Azure.Functions.Worker.Pipeline
{
    internal class DefaultInvocationPipelineBuilder<Context> : IInvocationPipelineBuilder<Context>
    {
        private readonly IList<Func<FunctionExecutionDelegate, FunctionExecutionDelegate>> _middlewareCollection =
            new List<Func<FunctionExecutionDelegate, FunctionExecutionDelegate>>();

        public IInvocationPipelineBuilder<Context> Use(Func<FunctionExecutionDelegate, FunctionExecutionDelegate> middleware)
        {
            _middlewareCollection.Add(middleware);

            return this;
        }

        public FunctionExecutionDelegate Build()
        {
            FunctionExecutionDelegate pipeline = context => Task.CompletedTask;

            pipeline = _middlewareCollection
                .Reverse()
                .Aggregate(pipeline, (p, d) => d(p));

            return pipeline;
        }
    }
}
