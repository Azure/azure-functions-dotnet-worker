using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.DotNetWorker
{
    public interface IFunctionInstanceFactory
    {
        object CreateInstance(Type type);
    }
}
