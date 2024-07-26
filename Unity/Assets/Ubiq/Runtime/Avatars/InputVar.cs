namespace Ubiq
{
    public readonly struct InputVar<T>
    {
        /// <summary>
        /// Convenience readonly for an invalid input variable.
        /// </summary>
        public static InputVar<T> invalid => new (default, valid: false);
        
        /// <summary>
        /// The value of the input variable. Should not be used if not valid.
        /// </summary>
        public T value { get; }
        /// <summary>
        /// Is the input var currently valid and available for use.
        /// </summary>
        public bool valid { get; }

        /// <summary>
        /// Create a new input variable. Can be invalid, indicating the variable
        /// is currently not provided and should not be used.
        /// </summary>
        /// <param name="value">The variable itself.</param>
        /// <param name="valid">Whether the variable can be used.</param>
        public InputVar(T value, bool valid = true)
        {
            this.value = value;
            this.valid = valid;
        }
    }
}