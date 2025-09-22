using Core.Interfaces;
using Microsoft.Extensions.Logging;
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

        /// <summary>
        /// 
        /// </summary>
        private readonly ILogger<GenericODataService<T>> _logger;

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
        /// <param name="logger">
        /// Optional logger for structured diagnostics. If not provided, no logging will occur.
        /// </param>
        public GenericODataService(
            HttpClient httpClient,
            string serviceName,
            ILogger<GenericODataService<T>> logger)
        {
            _httpClient = httpClient;
            _serviceName = serviceName;
            _logger = logger;
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

                _logger?.LogDebug("Fetching entities from {Url}", url);

                var result = await _httpClient.GetFromJsonAsync<ODataResponse<T>>(url, _jsonOptions);

                _logger?.LogInformation("Retrieved {Count} entities from {Service}",
                    result?.Value?.Count ?? 0, _serviceName);

                return result?.Value ?? new List<T>();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving entities from {Service}", _serviceName);
                throw new Exception($"Failed to retrieve entities: {ex.Message}", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<T> GetEntityByIdAsync(string filter)
        {
            try
            {
                var url = $"{_serviceName}?$filter={Uri.EscapeDataString(filter)}";
                _logger?.LogDebug("Fetching entity by filter from {Url}", url);

                var result = await _httpClient.GetFromJsonAsync<ODataResponse<T>>(url, _jsonOptions);

                var entity = result?.Value?.FirstOrDefault();

                if (entity == null)
                {
                    _logger?.LogWarning("No entity found in '{Service}' matching filter: {Filter}", _serviceName, filter);
                    throw new Exception($"No entity found in '{_serviceName}' matching filter: {filter}");
                }

                _logger?.LogInformation("Entity retrieved successfully from {Service}", _serviceName);
                return entity;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving entity from {Service}", _serviceName);
                throw new Exception($"Failed to retrieve entity by ID: {ex.Message}", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<T> CreateEntityAsync(T entity)
        {
            try
            {
                _logger?.LogDebug("Creating new entity in {Service}", _serviceName);

                var response = await _httpClient.PostAsJsonAsync(_serviceName, entity, _jsonOptions);
                response.EnsureSuccessStatusCode();

                var created = await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
                if (created == null)
                {
                    throw new Exception("Response body was empty or malformed.");
                }

                _logger?.LogInformation("Entity created successfully in {Service}", _serviceName);
                return created;
            }
            catch (HttpRequestException httpEx)
            {
                _logger?.LogError(httpEx, "HTTP error during creation in {Service}", _serviceName);
                throw new Exception($"HTTP error during creation: {httpEx.Message}", httpEx);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error creating entity in {Service}", _serviceName);
                throw new Exception($"Failed to create entity: {ex.Message}", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<T> UpdateEntityAsync(string key, T entity)
        {
            try
            {
                var etagProp = typeof(T).GetProperty("ETag");
                var etag = etagProp?.GetValue(entity) as string;

                var request = new HttpRequestMessage(HttpMethod.Patch, $"{_serviceName}('{key}')")
                {
                    Content = JsonContent.Create(entity, options: _jsonOptions)
                };

                if (!string.IsNullOrEmpty(etag))
                {
                    request.Headers.TryAddWithoutValidation("If-Match", etag);
                    _logger?.LogDebug("Including ETag header for concurrency control: {ETag}", etag);
                }

                _logger?.LogDebug("Updating entity {Key} in {Service}", key, _serviceName);

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var updated = await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
                if (updated == null)
                {
                    throw new Exception("Update succeeded but response body was empty or invalid.");
                }

                _logger?.LogInformation("Entity {Key} updated successfully in {Service}", key, _serviceName);
                return updated;
            }
            catch (HttpRequestException httpEx)
            {
                _logger?.LogError(httpEx, "HTTP error during update in {Service}", _serviceName);
                throw new Exception($"HTTP error during update: {httpEx.Message}", httpEx);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating entity in {Service}", _serviceName);
                throw new Exception($"Failed to update entity: {ex.Message}", ex);
            }
        }

        /// <inheritdoc/>
        public async Task DeleteEntityAsync(string key)
        {
            try
            {
                _logger?.LogDebug("Deleting entity {Key} from {Service}", key, _serviceName);

                var response = await _httpClient.DeleteAsync($"{_serviceName}('{key}')");
                response.EnsureSuccessStatusCode();

                _logger?.LogInformation("Entity {Key} deleted successfully from {Service}", key, _serviceName);
            }
            catch (HttpRequestException httpEx)
            {
                _logger?.LogError(httpEx, "HTTP error during deletion in {Service}", _serviceName);
                throw new Exception($"HTTP error during deletion: {httpEx.Message}", httpEx);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error deleting entity from {Service}", _serviceName);
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
