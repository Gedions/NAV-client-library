using Core.Interfaces;

namespace Infrastructure.Services
{
    /// <summary>
    /// Factory that creates <see cref="GenericODataService{T}"/> instances via an <see cref="IHttpClientFactory"/>.
    /// </summary>
    public class NavODataServiceFactory : INavODataServiceFactory
    {
        private readonly IHttpClientFactory _clientFactory;

        /// <summary>
        /// Initializes a new instance of <see cref="NavODataServiceFactory"/>.
        /// </summary>
        /// <param name="clientFactory">The HTTP client factory configured for NAV OData.</param>
        public NavODataServiceFactory(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        /// <inheritdoc/>
        public INavODataService<T> Create<T>(string? serviceName = null) where T : class
        {
            var client = _clientFactory.CreateClient("NavODataClient");
            return new GenericODataService<T>(client, serviceName);
        }
    }
}
