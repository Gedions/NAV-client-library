namespace Shared.Models
{
    /// <summary>
    /// Represents the outcome of a NAV SOAP operation, including status, messages, and any returned data.
    /// </summary>
    public class SoapResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether the SOAP call completed successfully.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets any informational or error message returned by the SOAP service.
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// Gets or sets the value returned by the SOAP operation, if any.
        /// This may be a primitive, a complex object, or null.
        /// </summary>
        public object? ReturnValue { get; set; }
    }
}
