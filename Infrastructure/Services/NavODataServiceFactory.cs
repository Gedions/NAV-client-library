using Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services
{
    /// <summary>
    /// Factory that creates <see cref="GenericODataService{T}"/> instances via an <see cref="IHttpClientFactory"/>.
    /// </summary>
    public class NavODataServiceFactory : INavODataServiceFactory
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly ILoggerFactory _loggerFactory;

        /// <summary>
        /// Initializes a new instance of <see cref="NavODataServiceFactory"/>.
        /// </summary>
        /// <param name="clientFactory">The HTTP client factory configured for NAV OData.</param>
        public NavODataServiceFactory(IHttpClientFactory clientFactory, ILoggerFactory loggerFactory)
        {
            _clientFactory = clientFactory;
            _loggerFactory = loggerFactory;
        }

        /// <inheritdoc/>
        public INavODataService<T> Create<T>(string? serviceName = null) where T : class
        {
            var client = _clientFactory.CreateClient("NavODataClient");
            var logger = _loggerFactory.CreateLogger<GenericODataService<T>>();

            return new GenericODataService<T>(client, serviceName ?? typeof(T).Name, logger);
        }
    }
}
