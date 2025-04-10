using System.Collections.Generic;

namespace ByteForge.Toolkit
{
    /// <summary>
    /// Represents a collection of validation errors.
    /// </summary>
    public class ValidationErrors : List<ValidationError>
    {
        /// <summary>
        /// Gets a value indicating whether there are any validation errors.
        /// </summary>
        public bool HasErrors => Count > 0;

        /// <summary>
        /// Adds a new validation error with the specified field and offending value.
        /// </summary>
        /// <param name="field">The field that caused the validation error.</param>
        /// <param name="offendingValue">The value that caused the validation error.</param>
        public void Add(string field, string offendingValue)
        {
            Add(new ValidationError(field, offendingValue));
        }

        /// <summary>
        /// Adds a new validation error with the specified field, message, and offending value.
        /// </summary>
        /// <param name="field">The field that caused the validation error.</param>
        /// <param name="message">The message describing the validation error.</param>
        /// <param name="offendingValue">The value that caused the validation error.</param>
        public void Add(string field, string message, string offendingValue)
        {
            Add(new ValidationError(field, message, offendingValue));
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return string.Join("\n", this);
        }
    }

    /// <summary>
    /// Represents a validation error.
    /// </summary>
    public class ValidationError
    {
        /// <summary>
        /// Gets or sets the field that caused the validation error.
        /// </summary>
        public string Field { get; set; }

        /// <summary>
        /// Gets or sets the message describing the validation error.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the value that caused the validation error.
        /// </summary>
        public string OffendingValue { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationError"/> class with the specified field and offending value.
        /// </summary>
        /// <param name="field">The field that caused the validation error.</param>
        /// <param name="offendingValue">The value that caused the validation error.</param>
        public ValidationError(string field, string offendingValue)
        {
            Field = field;
            OffendingValue = offendingValue;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationError"/> class with the specified field, message, and offending value.
        /// </summary>
        /// <param name="field">The field that caused the validation error.</param>
        /// <param name="message">The message describing the validation error.</param>
        /// <param name="offendingValue">The value that caused the validation error.</param>
        public ValidationError(string field, string message, string offendingValue)
        {
            Field = field;
            Message = message;
            OffendingValue = offendingValue;
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return $"{Field}: {Message ?? "Invalid value"} ({OffendingValue})";
        }
    }
}