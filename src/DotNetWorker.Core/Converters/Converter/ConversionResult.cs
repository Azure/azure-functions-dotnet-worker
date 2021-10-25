// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker.Converters
{
    /// <summary>
    /// A type representing the result of function input conversion operation.
    /// </summary>
    public readonly struct ConversionResult
    {
        // A static ConversionResult instance which represents an unhandled conversion.
        private static readonly ConversionResult _unhandledConversionResult = new(status: ConversionStatus.Unhandled);

        /// <summary>
        /// Creates a new <see cref="ConversionResult"/> instance.
        /// </summary>
        /// <param name="status">The status of conversion operation.</param>
        /// <param name="value">The value produced from the successful conversion.</param>
        /// <param name="error">The exception which caused the conversion to fail.</param>
        private ConversionResult(ConversionStatus status, object? value = null, Exception? error = null)
        {
            Status = status;
            Value = value;
            Error = error;
        }

        /// <summary>
        /// Gets the status of the conversion.
        /// </summary>
        public ConversionStatus Status { get; } 

        /// <summary>
        /// Gets the value produced from the conversion if it was successful.
        /// </summary>
        public object? Value { get; }

        /// <summary>
        /// Gets the error which caused the conversion to fail.
        /// </summary>
        public Exception? Error { get; }

        /// <summary>
        /// Creates a new <see cref="ConversionResult"/> instance to represent an unhandled input conversion.
        /// </summary>
        /// <returns>A new instance of <see cref="ConversionResult"/> where the Status property value is set to Unhandled.</returns>
        public static ConversionResult Unhandled() => _unhandledConversionResult;

        /// <summary>
        /// Creates a new <see cref="ConversionResult"/> instance to represent a successful input conversion.
        /// </summary>
        /// <returns>A new instance of <see cref="ConversionResult"/> to represent a successful conversion.</returns>
        public static ConversionResult Success(object? value) => new(status: ConversionStatus.Succeeded, value: value);

        /// <summary>
        /// Creates a new <see cref="ConversionResult"/> instance to represent a failed input conversion.
        /// </summary>
        /// <param name="exception">The exception representing the cause of the failed conversion.</param>
        /// <exception cref="ArgumentNullException">Throws when the <paramref name="exception"/> argument is null.</exception>
        /// <returns>A new instance of <see cref="ConversionResult"/> to represent a failed input conversion.</returns>
        public static ConversionResult Failed(Exception exception)
        {
            if (exception is null)
            {
                throw new ArgumentNullException(nameof(exception));
            }
            
            return new ConversionResult(status: ConversionStatus.Failed,
                                        value: null,
                                        error: exception);
        }
    }
}
