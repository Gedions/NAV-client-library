namespace Core.Interfaces
{
    /// <summary>
    /// Defines CRUD operations against an OData endpoint for entities of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    public interface INavODataService<T>
    {
        /// <summary>
        /// Retrieves a list of entities, optionally filtered.
        /// </summary>
        /// <param name="filter">An OData $filter expression, or <c>null</c>.</param>
        /// <param name="filters">Additional filter expressions to AND together, or <c>null</c>.</param>
        /// <returns>A task that resolves to the list of matching entities.</returns>
        Task<List<T>> GetEntitiesAsync(string? filter = null, string[]? filters = null);

        /// <summary>
        /// Retrieves a single entity matching the given key filter.
        /// </summary>
        /// <param name="filter">An OData $filter expression that uniquely identifies the entity.</param>
        /// <returns>A task that resolves to the matching entity.</returns>
        Task<T> GetEntityByIdAsync(string filter);

        /// <summary>
        /// Creates a new entity.
        /// </summary>
        /// <param name="entity">The entity to create.</param>
        /// <returns>
        /// A task that resolves to the created entity, 
        /// including any server-generated properties (e.g. keys).
        /// </returns>
        Task<T> CreateEntityAsync(T entity);

        /// <summary>
        /// Updates an existing entity.
        /// </summary>
        /// <param name="key">The OData key of the entity to update.</param>
        /// <param name="entity">The entity data to apply.</param>
        /// <returns>A task that resolves to the updated entity.</returns>
        Task<T> UpdateEntityAsync(string key, T entity);

        /// <summary>
        /// Deletes an entity by its key.
        /// </summary>
        /// <param name="key">The OData key of the entity to delete.</param>
        /// <returns>A task that completes once deletion is done.</returns>
        Task DeleteEntityAsync(string key);
    }
}
