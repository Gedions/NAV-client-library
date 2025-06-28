using Core.Interfaces;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Infrastructure.Services
{
    /// <summary>
    /// Generic implementation of <see cref="INavODataService{T}"/>, using HttpClient to communicate
    /// with a NAV OData endpoint for CRUD operations.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    public class GenericODataService<T> : INavODataService<T> where T : class
    {
        /// <summary>
        /// The HttpClient instance configured for OData requests.
        /// </summary>
        protected readonly HttpClient _httpClient;

        /// <summary>
        /// The OData service name (conventionally the entity set name in NAV).
        /// </summary>
        protected readonly string _serviceName;

        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// Initializes a new instance of <see cref="GenericODataService{T}"/>.
        /// </summary>
        /// <param name="httpClient">An <see cref="HttpClient"/> configured for the NAV OData endpoint.</param>
        /// <param name="serviceName">
        /// Optional service name; if null, uses the CLR type name of <typeparamref name="T"/>.
        /// </param>
        public GenericODataService(HttpClient httpClient, string? serviceName = null)
        {
            _httpClient = httpClient;
            _serviceName = serviceName ?? typeof(T).Name;
        }

        /// <inheritdoc/>
        public async Task<List<T>> GetEntitiesAsync(string? filter = null, string[]? filters = null)
        {
            try
            {
                var filterParts = new List<string>();
                if (!string.IsNullOrEmpty(filter))
                    filterParts.Add(filter);

                if (filters is { Length: > 0 })
                    filterParts.AddRange(filters.Where(f => !string.IsNullOrEmpty(f)));

                string? finalFilter = filterParts.Count > 0
                    ? string.Join(" and ", filterParts)
                    : null;

                var url = _serviceName;
                if (!string.IsNullOrEmpty(finalFilter))
                    url += $"?$filter={Uri.EscapeDataString(finalFilter)}";

                var result = await _httpClient.GetFromJsonAsync<ODataResponse<T>>(url, _jsonOptions);
                return result?.Value ?? new List<T>();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to retrieve entities: {ex.Message}", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<T> GetEntityByIdAsync(string filter)
        {
            try
            {
                var result = await _httpClient.GetFromJsonAsync<ODataResponse<T>>(
                    $"{_serviceName}?$filter={Uri.EscapeDataString(filter)}",
                    _jsonOptions);

                return result?.Value?.FirstOrDefault()
                    ?? throw new Exception($"No entity found in '{_serviceName}' matching filter: {filter}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to retrieve entity by ID: {ex.Message}", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<T> CreateEntityAsync(T entity)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(_serviceName, entity, _jsonOptions);
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadFromJsonAsync<T>(_jsonOptions)
                    ?? throw new Exception("Response body was empty or malformed.");
            }
            catch (HttpRequestException httpEx)
            {
                throw new Exception($"HTTP error during creation: {httpEx.Message}", httpEx);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create entity: {ex.Message}", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<T> UpdateEntityAsync(string key, T entity)
        {
            try
            {
                // If T has an ETag property, include it for concurrency control
                var etagProp = typeof(T).GetProperty("ETag");
                var etag = etagProp?.GetValue(entity) as string;

                var request = new HttpRequestMessage(HttpMethod.Patch, $"{_serviceName}('{key}')")
                {
                    Content = JsonContent.Create(entity, options: _jsonOptions)
                };

                if (!string.IsNullOrEmpty(etag))
                    request.Headers.TryAddWithoutValidation("If-Match", etag);

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadFromJsonAsync<T>(_jsonOptions)
                    ?? throw new Exception("Update succeeded but response body was empty or invalid.");
            }
            catch (HttpRequestException httpEx)
            {
                throw new Exception($"HTTP error during update: {httpEx.Message}", httpEx);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to update entity: {ex.Message}", ex);
            }
        }

        /// <inheritdoc/>
        public async Task DeleteEntityAsync(string key)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{_serviceName}('{key}')");
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException httpEx)
            {
                throw new Exception($"HTTP error during deletion: {httpEx.Message}", httpEx);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to delete entity: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Wrapper for deserializing OData JSON payloads.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    public class ODataResponse<T>
    {
        /// <summary>
        /// The OData context URL returned by the service.
        /// </summary>
        [JsonPropertyName("@odata.context")]
        public string? Context { get; set; }

        /// <summary>
        /// The collection of entities.
        /// </summary>
        [JsonPropertyName("value")]
        public List<T>? Value { get; set; }
    }
}
