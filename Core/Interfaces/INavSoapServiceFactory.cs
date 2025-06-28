namespace Core.Interfaces
{
    /// <summary>
    /// Factory for creating <see cref="INavSoapService{T}"/> instances.
    /// </summary>
    public interface INavSoapServiceFactory
    {
        /// <summary>
        /// Creates a service for SOAP entities of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The SOAP model type.</typeparam>
        /// <param name="serviceName">
        /// Optional SOAP service name (defaults to convention-based name if <c>null</c>).
        /// </param>
        /// <returns>An <see cref="INavSoapService{T}"/> instance.</returns>
        INavSoapService<T> Create<T>(string? serviceName = null) where T : class;
    }
}
