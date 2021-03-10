using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Worker.Diagnostics.Exceptions
{
    internal class FunctionInputConverterException : FunctionWorkerException
    {
        internal FunctionInputConverterException(string message) : base(message) { }

        internal FunctionInputConverterException(string message, Exception innerException) : base(message, innerException) { }
    }
}
