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
                        extractedResult = str.getValue();
                        break;
                    case OutputBinding<int> num:
                        extractedResult = num.getValue();
                        break;
                    case OutputBinding<HttpResponseData> httpResponse:
                        extractedResult = httpResponse.getValue();
                        break;

                    //TODO: lists of types and other types we might need to support?
                }

                return extractedResult; 
            }

            throw new Exception("Cannot extract value from an object that is not an OutputBinding.");
        }
    }
}
