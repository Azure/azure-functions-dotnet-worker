using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FunctionsDotNetWorker.Converters
{
    public interface IParameterConverter
    {
        bool TryConvert(object source, Type targetType, string name, out object target);
    }
}
