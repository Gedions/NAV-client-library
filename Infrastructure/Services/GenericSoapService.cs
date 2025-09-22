using Core.Interfaces;
using Shared.Models;
using Shared.Utilities;
using System.Text;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Infrastructure.Services
{
    /// <summary>
    /// Generic implementation of <see cref="INavSoapService{T}"/>, issuing SOAP requests
    /// to a NAV SOAP endpoint and parsing responses.
    /// </summary>
    /// <typeparam name="T">The model type representing the SOAP entity.</typeparam>
    public class GenericSoapService<T> : INavSoapService<T> where T : class
    {
        private readonly HttpClient _httpClient;
        private readonly string _serviceName;
        private readonly ILogger<GenericSoapService<T>> _logger;

        /// <summary>
        /// Gets the SOAP namespace for the current service.
        /// </summary>
        private string Ns => $"urn:microsoft-dynamics-schemas/page/{_serviceName.ToLower()}";

        /// <summary>
        /// Initializes a new instance of <see cref="GenericSoapService{T}"/>.
        /// </summary>
        /// <param name="httpClient">An <see cref="HttpClient"/> configured for the NAV SOAP endpoint.</param>
        /// <param name="logger">Logger instance for structured logging.</param>
        /// <param name="serviceName">
        /// The NAV SOAP service name; if null, uses the CLR type name of <typeparamref name="T"/>.
        /// </param>
        public GenericSoapService(
             HttpClient httpClient,
             string serviceName,
             ILogger<GenericSoapService<T>> logger)
        {
            _httpClient = httpClient;
            _serviceName = serviceName;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<List<T>> ReadAllAsync(XElement filtersXml)
        {
            _logger.LogInformation("Reading all records from {Service}", _serviceName);

            var body = new XElement(XName.Get("ReadMultiple", Ns),
                filtersXml,
                new XElement(XName.Get("setSize", Ns), 1000)
            );

            var envelope = SoapEnvelopeBuilder.BuildEnvelope(body);

            _logger.LogDebug("Envelop Content {Envelope}", envelope.ToString());

            var response = await SendSoapRequestAsync(envelope.ToString(), "page/ReadMultiple");

            return SoapResponseParser.ParseReadMultiple<T>(response);
        }

        /// <inheritdoc/>
        public async Task<T?> ReadAsync(XElement keyFieldsXml)
        {
            _logger.LogInformation("Reading single record from {Service}", _serviceName);

            var envelope = SoapEnvelopeBuilder.BuildEnvelope(keyFieldsXml);

            _logger.LogDebug("Envelope Content {Envelope}", envelope.ToString());

            var response = await SendSoapRequestAsync(envelope.ToString(), "page/Read");

            return SoapResponseParser.ParseRead<T>(response);
        }

        /// <inheritdoc/>
        public async Task<T?> CreateAsync(XElement createPayloadXml)
        {
            _logger.LogInformation("Creating record in {Service}", _serviceName);

            var envelope = SoapEnvelopeBuilder.BuildEnvelope(createPayloadXml);

            _logger.LogDebug("Envelope Content {Envelope}", envelope.ToString());

            var response = await SendSoapRequestAsync(envelope.ToString(), "page/Create");

            return SoapResponseParser.ParseCreateOrUpdate<T>(response);
        }

        /// <inheritdoc/>
        public async Task<T?> UpdateAsync(XElement updatePayloadXml)
        {
            _logger.LogInformation("Updating record in {Service}", _serviceName);

            var envelope = SoapEnvelopeBuilder.BuildEnvelope(updatePayloadXml);

            _logger.LogDebug("Envelope Content {Envelope}", envelope.ToString());

            var response = await SendSoapRequestAsync(envelope.ToString(), "page/Update");

            return SoapResponseParser.ParseCreateOrUpdate<T>(response);
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteAsync(XElement keyFieldsXml)
        {
            _logger.LogWarning("Deleting record in {Service}", _serviceName);

            var envelope = SoapEnvelopeBuilder.BuildEnvelope(keyFieldsXml);

            _logger.LogDebug("Envelope Content {Envelope}", envelope.ToString());

            var response = await SendSoapRequestAsync(envelope.ToString(), "page/Delete");

            return SoapResponseParser.ParseDelete(response);
        }

        /// <inheritdoc/>
        public async Task<SoapResult> InvokeCodeunitAsync(string serviceName, string methodName, XElement parametersXml)
        {
            _logger.LogDebug("Invoking codeunit {Service}.{Method}", serviceName, methodName);

            var envelope = SoapEnvelopeBuilder.BuildEnvelope(parametersXml);

            _logger.LogDebug("Envelope Content {Envelope}", envelope.ToString());

            var response = await SendSoapCodeunitRequestAsync(serviceName, envelope.ToString(), $"codeunit/{serviceName}");
            
            return SoapResponseParser.ParseReadCodeunit(response, methodName, _serviceName);
        }

        private async Task<string> SendSoapRequestAsync(string soapEnvelope, string soapAction)
        {
            var url = $"{_httpClient.BaseAddress}{_serviceName}";
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml")
            };
            request.Headers.Add("SOAPAction", $"urn:microsoft-dynamics-schemas/{soapAction}");

            _logger.LogDebug("Sending SOAP request {Action} to {Url}", soapAction, url);

            try
            {
                var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    var fault = TryExtractSoapFault(content);

                    _logger.LogError("SOAP error {StatusCode}: {Fault}", (int)response.StatusCode, fault ?? content);
                    
                    throw new Exception($"SOAP Error: HTTP {(int)response.StatusCode} - {fault ?? content}");
                }

                if (content.Contains("<faultcode>") || content.Contains("<Fault>"))
                {
                    var fault = TryExtractSoapFault(content);
                    
                    _logger.LogError("SOAP fault in {Service}: {Fault}", _serviceName, fault);
                    
                    throw new Exception($"SOAP Fault: {fault}");
                }

                _logger.LogDebug("SOAP response received successfully from {Service}", _serviceName);

                return content;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP Request failed for {Service}", _serviceName);

                throw new Exception($"HTTP Request failed: {ex.Message}", ex);
            }
        }

        private async Task<string> SendSoapCodeunitRequestAsync(string serviceName, string soapEnvelope, string soapAction)
        {
            var url = $"{_httpClient.BaseAddress}{serviceName}".Replace("/Page/", "/Codeunit/");
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml")
            };
            request.Headers.Add("SOAPAction", $"urn:microsoft-dynamics-schemas/{soapAction}");

            _logger.LogDebug("Sending SOAP codeunit request {Action} to {Url}", soapAction, url);

            try
            {
                var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    var fault = TryExtractSoapFault(content);
                    _logger.LogError("SOAP error {StatusCode}: {Fault}", (int)response.StatusCode, fault ?? content);
                    throw new Exception($"SOAP Error: HTTP {(int)response.StatusCode} - {fault ?? content}");
                }

                if (content.Contains("<faultcode>") || content.Contains("<Fault>"))
                {
                    var fault = TryExtractSoapFault(content);
                    _logger.LogError("SOAP fault in codeunit {Service}: {Fault}", serviceName, fault);
                    throw new Exception($"SOAP Fault: {fault}");
                }

                _logger.LogDebug("SOAP codeunit response received successfully from {Service}", serviceName);
                return content;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP Request failed for codeunit {Service}", serviceName);
                throw new Exception($"HTTP Request failed: {ex.Message}", ex);
            }
        }

        private static string? TryExtractSoapFault(string xml)
        {
            try
            {
                var doc = XDocument.Parse(xml);
                var ns = doc.Root?.Name.Namespace;
                var faultString = doc.Descendants(ns! + "faultstring").FirstOrDefault()?.Value
                                  ?? doc.Descendants("faultstring").FirstOrDefault()?.Value;
                var detail = doc.Descendants(ns! + "detail").FirstOrDefault()?.Value
                             ?? doc.Descendants("detail").FirstOrDefault()?.Value;
                return faultString ?? detail;
            }
            catch
            {
                return null;
            }
        }
    }
}
