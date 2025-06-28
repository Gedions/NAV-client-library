using Shared.Models;
using System.Xml.Linq;

namespace Core.Interfaces
{
    /// <summary>
    /// Defines CRUD and codeunit invocation operations against a NAV SOAP service for type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The model type representing the SOAP entity.</typeparam>
    public interface INavSoapService<T> where T : class
    {
        /// <summary>
        /// Reads multiple records matching the given filters.
        /// </summary>
        /// <param name="filtersXml">
        /// An <see cref="XElement"/> containing <c>Field</c> elements to filter by.
        /// </param>
        /// <returns>A task resolving to the list of matching records.</returns>
        Task<List<T>> ReadAllAsync(XElement filtersXml);

        /// <summary>
        /// Reads a single record by its key fields.
        /// </summary>
        /// <param name="keyFieldsXml">
        /// An <see cref="XElement"/> containing the key fields for the record.
        /// </param>
        /// <returns>
        /// A task resolving to the entity, or <c>null</c> if not found.
        /// </returns>
        Task<T?> ReadAsync(XElement keyFieldsXml);

        /// <summary>
        /// Creates a new record.
        /// </summary>
        /// <param name="createPayloadXml">
        /// An <see cref="XElement"/> representing the fields for the new record.
        /// </param>
        /// <returns>
        /// A task resolving to the created entity, or <c>null</c> on failure.
        /// </returns>
        Task<T?> CreateAsync(XElement createPayloadXml);

        /// <summary>
        /// Updates an existing record.
        /// </summary>
        /// <param name="updatePayloadXml">
        /// An <see cref="XElement"/> with key and updated fields.
        /// </param>
        /// <returns>
        /// A task resolving to the updated entity, or <c>null</c> on failure.
        /// </returns>
        Task<T?> UpdateAsync(XElement updatePayloadXml);

        /// <summary>
        /// Deletes a record by its key fields.
        /// </summary>
        /// <param name="keyFieldsXml">
        /// An <see cref="XElement"/> containing the key fields to delete.
        /// </param>
        /// <returns>
        /// A task resolving to <c>true</c> if deletion succeeded; otherwise <c>false</c>.
        /// </returns>
        Task<bool> DeleteAsync(XElement keyFieldsXml);

        /// <summary>
        /// Invokes a NAV codeunit method.
        /// </summary>
        /// <param name="serviceName">The name of the SOAP service.</param>
        /// <param name="methodName">The codeunit method to call.</param>
        /// <param name="parametersXml">
        /// An <see cref="XElement"/> containing parameters for the method.
        /// </param>
        /// <returns>
        /// A task resolving to a <see cref="SoapResult"/> containing any return values or errors.
        /// </returns>
        Task<SoapResult> InvokeCodeunitAsync(string serviceName, string methodName, XElement parametersXml);
    }
}
