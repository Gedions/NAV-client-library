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
        /// Reads multiple records from the NAV/BC service that match the specified filters.
        /// </summary>
        /// <param name="filters">
        /// A collection of <see cref="XElement"/> objects representing the filters to apply.  
        /// Each element should contain a <c>Field</c> and a <c>Criteria</c> node.
        /// </param>
        /// <param name="bookmarkKey">
        /// An optional bookmark key used for pagination when fetching subsequent result sets.  
        /// If <c>null</c>, the request retrieves records from the beginning.
        /// </param>
        /// <param name="setSize">
        /// The maximum number of records to return in the result set.  
        /// If set to <c>0</c>, the default NAV/BC page size is used.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.  
        /// The task result contains a list of records matching the given filters.
        /// </returns>
        Task<List<T>> ReadAllAsync(IEnumerable<XElement> filters = null, string bookmarkKey = null, int setSize = 0);

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
