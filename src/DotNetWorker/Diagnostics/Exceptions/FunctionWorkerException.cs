using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Worker.Diagnostics
{
    /// <summary>
    /// Internal exception that is surfaced to the user
    /// </summary>
    internal class FunctionWorkerException : Exception
    {
        internal FunctionWorkerException(string message) : base(message) { }

        internal FunctionWorkerException(string message, Exception innerException) : base(message, innerException) { }
    }
}
