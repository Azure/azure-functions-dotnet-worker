using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FunctionsDotNetWorker
{
    public class OutputBinding<T>
    {
        private T val;

        public T getValue()
        {
            return val;
        }

        public void setValue(T value)
        {
            val = value; 
        }
    }
}
