// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker.Core
{
    internal static class ExceptionExtensions
    {
        public static bool IsFatal(this Exception? exception)
        {
            while (exception is not null)
            {
                if (exception 
                    is (OutOfMemoryException and not InsufficientMemoryException)
                    or AppDomainUnloadedException
                    or BadImageFormatException
                    or CannotUnloadAppDomainException
                    or InvalidProgramException
                    or AccessViolationException)
                {
                    return true;
                }

                exception = exception.InnerException;
            }

            return false;
        }
    }
}
