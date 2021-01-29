using System;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Definition;
using Microsoft.Azure.Functions.Worker.Pipeline;

namespace Microsoft.Azure.Functions.Worker.Tests.Converters
{
    internal class TestConverterContext : ConverterContext
    {
        public TestConverterContext(string paramName, Type paramType, object source, FunctionExecutionContext context = null)
        {
            Parameter = new FunctionParameter(paramName, paramType);
            Source = source;
            ExecutionContext = context ?? new TestFunctionExecutionContext();
        }

        public override FunctionParameter Parameter { get; set; }

        public override object Source { get; set; }

        public override FunctionExecutionContext ExecutionContext { get; set; }
    }
}
