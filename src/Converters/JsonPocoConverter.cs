using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace FunctionsDotNetWorker.Converters
{
    class JsonPocoConverter : IParameterConverter
    {
        public bool TryConvert(object source, Type targetType, string name, out object target)
        {
            target = null;

            if (!(source is string stringData))
            {
                return false;
            }

            try
            {
                target = JsonSerializer.Deserialize(stringData, targetType);
                return true;
            }
            catch
            {
                return false; 
            }
        }
    }
}
