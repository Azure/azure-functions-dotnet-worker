using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FunctionsDotNetWorker.Converters
{
    public class OutBindingConverter
    {
        public static object ExtractValue(object bindingVar)
        {
            var bindingVarType = bindingVar.GetType();
            var genericType = bindingVarType.GetGenericTypeDefinition();

            if(genericType == typeof(OutputBinding<>))
            {
                object extractedResult = null; 
                switch (bindingVar)
                {
                    case OutputBinding<string> str:
                        var extractedStr = (OutputBinding<string>)bindingVar;
                        extractedResult = extractedStr.getValue();
                        break;
                    case OutputBinding<int> num:
                        var extractedInt= (OutputBinding<int>)bindingVar;
                        extractedResult = extractedInt.getValue();
                        break;
                    //TODO: lists of types and other types we might need to support?
                }

                return extractedResult; 
            }

            throw new Exception("Cannot extract value from an object that is not an OutputBinding.");
        }
    }
}
