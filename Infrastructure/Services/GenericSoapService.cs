using Core.Interfaces;
using Shared.Models;
using Shared.Utilities;
using System.Text;
using System.Xml.Linq;

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

        /// <summary>
        /// Gets the SOAP namespace for the current service.
        /// </summary>
        private string Ns => $"urn:microsoft-dynamics-schemas/page/{_serviceName.ToLower()}";

        /// <summary>
        /// Initializes a new instance of <see cref="GenericSoapService{T}"/>.
        /// </summary>
        /// <param name="httpClient">An <see cref="HttpClient"/> configured for the NAV SOAP endpoint.</param>
        /// <param name="serviceName">
        /// The NAV SOAP service name; if null, uses the CLR type name of <typeparamref name="T"/>.
        /// </param>
        public GenericSoapService(HttpClient httpClient, string serviceName)
        {
            _httpClient = httpClient;
            _serviceName = serviceName;
        }

        /// <inheritdoc/>
        public async Task<List<T>> ReadAllAsync(XElement filtersXml)
        {
            var body = new XElement(XName.Get("ReadMultiple", Ns),
                filtersXml,
                new XElement(XName.Get("setSize", Ns), 1000)
            );
            var envelope = SoapEnvelopeBuilder.BuildEnvelope(body);
            var response = await SendSoapRequestAsync(envelope.ToString(), "page/ReadMultiple");
            return SoapResponseParser.ParseReadMultiple<T>(response);
        }

        /// <inheritdoc/>
        public async Task<T?> ReadAsync(XElement keyFieldsXml)
        {
            var envelope = SoapEnvelopeBuilder.BuildEnvelope(keyFieldsXml);
            var response = await SendSoapRequestAsync(envelope.ToString(), "page/Read");
            return SoapResponseParser.ParseRead<T>(response);
        }

        /// <inheritdoc/>
        public async Task<T?> CreateAsync(XElement createPayloadXml)
        {
            var envelope = SoapEnvelopeBuilder.BuildEnvelope(createPayloadXml);
            var response = await SendSoapRequestAsync(envelope.ToString(), "page/Create");
            return SoapResponseParser.ParseCreateOrUpdate<T>(response);
        }

        /// <inheritdoc/>
        public async Task<T?> UpdateAsync(XElement updatePayloadXml)
        {
            var envelope = SoapEnvelopeBuilder.BuildEnvelope(updatePayloadXml);
            var response = await SendSoapRequestAsync(envelope.ToString(), "page/Update");
            return SoapResponseParser.ParseCreateOrUpdate<T>(response);
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteAsync(XElement keyFieldsXml)
        {
            var envelope = SoapEnvelopeBuilder.BuildEnvelope(keyFieldsXml);
            var response = await SendSoapRequestAsync(envelope.ToString(), "page/Delete");
            return SoapResponseParser.ParseDelete(response);
        }

        /// <inheritdoc/>
        public async Task<SoapResult> InvokeCodeunitAsync(string serviceName, string methodName, XElement parametersXml)
        {
            var envelope = SoapEnvelopeBuilder.BuildEnvelope(parametersXml);
            var response = await SendSoapCodeunitRequestAsync(serviceName, envelope.ToString(), $"codeunit/{serviceName}");
            return SoapResponseParser.ParseReadCodeunit(response, methodName, _serviceName);
        }

        /// <summary>
        /// Sends a SOAP request to a NAV page endpoint.
        /// </summary>
        /// <param name="soapEnvelope">The full SOAP envelope XML.</param>
        /// <param name="soapAction">The SOAPAction header (e.g. "page/Read").</param>
        /// <returns>The raw XML response as a string.</returns>
        /// <exception cref="Exception">Thrown on HTTP errors or SOAP faults.</exception>
        private async Task<string> SendSoapRequestAsync(string soapEnvelope, string soapAction)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{_httpClient.BaseAddress}{_serviceName}")
            {
                Content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml")
            };
            request.Headers.Add("SOAPAction", $"urn:microsoft-dynamics-schemas/{soapAction}");

            try
            {
                var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    var fault = TryExtractSoapFault(content);
                    throw new Exception($"SOAP Error: HTTP {(int)response.StatusCode} - {fault ?? content}");
                }
                if (content.Contains("<faultcode>") || content.Contains("<Fault>"))
                {
                    var fault = TryExtractSoapFault(content);
                    throw new Exception($"SOAP Fault: {fault}");
                }
                return content;
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"HTTP Request failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Sends a SOAP request to a NAV codeunit endpoint.
        /// </summary>
        private async Task<string> SendSoapCodeunitRequestAsync(string serviceName, string soapEnvelope, string soapAction)
        {
            var url = $"{_httpClient.BaseAddress}{serviceName}".Replace("/Page/", "/Codeunit/");
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml")
            };
            request.Headers.Add("SOAPAction", $"urn:microsoft-dynamics-schemas/{soapAction}");

            try
            {
                var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    var fault = TryExtractSoapFault(content);
                    throw new Exception($"SOAP Error: HTTP {(int)response.StatusCode} - {fault ?? content}");
                }
                if (content.Contains("<faultcode>") || content.Contains("<Fault>"))
                {
                    var fault = TryExtractSoapFault(content);
                    throw new Exception($"SOAP Fault: {fault}");
                }
                return content;
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"HTTP Request failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Parses a SOAP fault string out of the response XML, if present.
        /// </summary>
        /// <param name="xml">The SOAP response.</param>
        /// <returns>The fault message or null if none found.</returns>
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
