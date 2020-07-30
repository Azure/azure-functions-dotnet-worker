using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.DotNetWorker.Pipeline
{
    public class DefaultInvocationPipelineBuilder<FunctionExecutionContext> : IInvocationPipelineBuilder<FunctionExecutionContext>
    {
        private readonly IList<Func<EventDelegate<FunctionExecutionContext>, EventDelegate<FunctionExecutionContext>>> _middlewareCollection = 
            new List<Func<EventDelegate<FunctionExecutionContext>, EventDelegate<FunctionExecutionContext>>>();

        public IInvocationPipelineBuilder<FunctionExecutionContext> Use(Func<EventDelegate<FunctionExecutionContext>, EventDelegate<FunctionExecutionContext>> middleware)
        {
            _middlewareCollection.Add(middleware);

            return this;
        }

        public EventDelegate<FunctionExecutionContext> Build()
        {
            EventDelegate<FunctionExecutionContext> pipeline = context => Task.CompletedTask;

            pipeline = _middlewareCollection
                .Reverse()
                .Aggregate(pipeline, (p, d) => d(p));

            return pipeline;
        }
    }
}
