using System;

namespace Microsoft.Azure.Functions.Worker
{
    static class Guard
    {
        public static void AgainstNull(string argumentName, object? value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(argumentName);
            }
        }
    }
}
