// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace System;

/// <summary>
/// Extensions for exceptions.
/// </summary>
internal static class ExceptionExtensions
{
    /// <summary>
    /// Determines whether the exception is considered fatal and should not be caught.
    /// </summary>
    /// <param name="exception">The exception to check.</param>
    /// <returns></returns>
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

            if (exception is AggregateException aggregate)
            {
                foreach (Exception inner in aggregate.InnerExceptions)
                {
                    if (inner.IsFatal())
                    {
                        return true;
                    }
                }
            }
            else
            {
                exception = exception.InnerException;
            }
        }

        return false;
    }
}
