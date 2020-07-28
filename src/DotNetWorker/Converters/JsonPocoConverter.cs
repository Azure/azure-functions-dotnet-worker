using System;
using System.Text.Json;

namespace Microsoft.Azure.Functions.DotNetWorker.Converters
{
    public class JsonPocoConverter : IParameterConverter
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
