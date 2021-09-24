namespace Microsoft.Azure.Functions.Worker.Converters
{
    /// <summary>
    /// A type representing the result of function input conversion operation.
    /// </summary>
    public readonly struct ConversionResult
    {
        /// <summary>
        /// Gets a value indicating whether the conversion operation was successful or not.
        /// </summary>
        public bool IsSuccess { get; }

        /// <summary>
        /// Gets the value produced from the conversion operation if it was succesful.
        /// </summary>
        public object? Model {get;}

        /// <summary>
        /// Creates a new <see cref="ConversionResult"/> instance.
        /// </summary>
        /// <param name="isSuccess">Indicates the conversion operation was successful or not</param>
        /// <param name="model">The value produced from the successful conversion.</param>
        public ConversionResult(bool isSuccess, object? model)
        {
            IsSuccess = isSuccess;
            Model = model;
        }

        /// <summary>
        /// Creates a new <see cref="ConversionResult"/> instance.
        /// </summary>
        public ConversionResult(object model)
        {
            IsSuccess = true;
            Model = model;
        }

        /// <summary>
        /// Creates a new <see cref="ConversionResult"/> instance to represent a succesful conversion.
        /// </summary>
        /// <returns>A new instance of new <see cref="ConversionResult"/>.</returns>
        public static ConversionResult Success(object? model) => new ConversionResult(isSuccess: true, model);
                
        /// <summary>
        /// Creates a new <see cref="ConversionResult"/> instance to represent a failed conversion.
        /// </summary>
        /// <returns>A new instance of new <see cref="ConversionResult"/>.</returns>
        public static ConversionResult Failed() => new ConversionResult(isSuccess:false, model: null);
    }
}
