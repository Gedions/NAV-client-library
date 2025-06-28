using Core.Interfaces;

namespace Infrastructure.Services
{
    /// <summary>
    /// Factory that creates <see cref="GenericSoapService{T}"/> instances via an <see cref="IHttpClientFactory"/>.
    /// </summary>
    public class NavSoapServiceFactory : INavSoapServiceFactory
    {
        private readonly IHttpClientFactory _clientFactory;

        /// <summary>
        /// Initializes a new instance of <see cref="NavSoapServiceFactory"/>.
        /// </summary>
        /// <param name="clientFactory">The HTTP client factory configured for NAV SOAP.</param>
        public NavSoapServiceFactory(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        /// <inheritdoc/>
        public INavSoapService<T> Create<T>(string? serviceName = null) where T : class
        {
            var client = _clientFactory.CreateClient("NavSoapClient");
            return new GenericSoapService<T>(client, serviceName!);
        }
    }
}
