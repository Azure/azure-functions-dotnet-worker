using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Converters;

namespace Microsoft.Azure.Functions.Worker.Tests.Converters
{
    internal class TestConverterContext : ConverterContext
    {
        public TestConverterContext(Type targetType, object source, FunctionContext context = null)
        {
            TargetType = targetType;
            Source = source;
            FunctionContext = context; //TODO: SharedTestFunctionContext ?? new TestFunctionContext();
        }

        public override object Source { get; }

        public override FunctionContext FunctionContext { get; }

        public override Type TargetType { get; }

        public override IReadOnlyDictionary<string, object> Properties { get; }
    }
}
