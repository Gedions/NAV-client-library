namespace Core.Interfaces
{
    /// <summary>
    /// Factory for creating <see cref="INavODataService{T}"/> instances.
    /// </summary>
    public interface INavODataServiceFactory
    {
        /// <summary>
        /// Creates a service for entities of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="serviceName">
        /// Optional NAV OData service name (defaults to convention-based name if <c>null</c>).
        /// </param>
        /// <returns>An <see cref="INavODataService{T}"/> instance.</returns>
        INavODataService<T> Create<T>(string? serviceName = null) where T : class;
    }
}
