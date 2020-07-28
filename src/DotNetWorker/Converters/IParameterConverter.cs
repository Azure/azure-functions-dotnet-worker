using System;

namespace Microsoft.Azure.Functions.DotNetWorker.Converters
{
    public interface IParameterConverter
    {
        bool TryConvert(object source, Type targetType, string name, out object target);
    }
}
