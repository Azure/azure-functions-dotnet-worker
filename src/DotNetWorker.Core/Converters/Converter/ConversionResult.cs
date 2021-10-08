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
        private static readonly ConversionResult _unhandledConversionResult = new(isHandled: false);
        
        /// <summary>
        /// Gets a value indicating whether the converter acted on the input to execute a conversion operation.
        /// </summary>
        public bool IsHandled { get; }

        /// <summary>
        /// Gets a value indicating whether the conversion operation was successful or not.
        /// </summary>
        public bool? IsSuccessful { get; }

        /// <summary>
        /// Gets the value produced from the conversion operation if it was successful.
        /// </summary>
        public object? Value { get; }

        /// <summary>
        /// Gets the error which caused the conversion to fail.
        /// If the conversion was successful or was not handled, this will return null. 
        /// </summary>
        public Exception? Error { get; }

        /// <summary>
        /// Creates a new <see cref="ConversionResult"/> instance.
        /// </summary>
        /// <param name="isHandled">Indicates whether the converter acted on the input.</param>
        /// <param name="isSuccessful">Indicates whether the conversion operation was successful or not.</param>
        /// <param name="value">The value produced from the successful conversion.</param>
        /// <param name="error">The exception which caused the conversion to fail.</param>
        private ConversionResult(bool isHandled, bool? isSuccessful = null, object? value = null, Exception? error = null)
        {
            IsHandled = isHandled;
            IsSuccessful = isSuccessful;
            Value = value;
            Error = error;
        }

        /// <summary>
        /// Creates a new <see cref="ConversionResult"/> instance to represent an unhandled input conversion.
        /// </summary>
        /// <returns>A new instance of <see cref="ConversionResult"/> where the IsHandled property value is set to false.</returns>
        public static ConversionResult Unhandled() => _unhandledConversionResult;

        /// <summary>
        /// Creates a new <see cref="ConversionResult"/> instance to represent a successful input conversion.
        /// </summary>
        /// <returns>A new instance of <see cref="ConversionResult"/> to represent a successful conversion.</returns>
        public static ConversionResult Success(object? value) => new(isHandled: true, isSuccessful: true, value: value);

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
            
            return new ConversionResult(isHandled: true,
                                        isSuccessful: false,
                                        value: null,
                                        error: exception);
        }
    }
}
