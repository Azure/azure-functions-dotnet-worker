using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.Functions.Worker
{
    [AttributeUsage(AttributeTargets.Method)]
    public abstract class RetryAttribute : Attribute
    {
        public RetryAttribute()
        {
        }
    }
}
