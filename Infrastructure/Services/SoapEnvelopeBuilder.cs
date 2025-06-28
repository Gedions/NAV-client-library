using System.Xml.Linq;

namespace Infrastructure.Services
{
    /// <summary>
    /// Utility to build a SOAP envelope around a given body element.
    /// </summary>
    public static class SoapEnvelopeBuilder
    {
        private const string SoapNs = "http://schemas.xmlsoap.org/soap/envelope/";

        /// <summary>
        /// Wraps the provided <paramref name="body"/> element in a SOAP Envelope.
        /// </summary>
        /// <param name="body">The SOAP body payload.</param>
        /// <returns>An <see cref="XElement"/> representing the full SOAP envelope.</returns>
        public static XElement BuildEnvelope(XElement body)
        {
            return new XElement(
                XName.Get("Envelope", SoapNs),
                new XAttribute(XNamespace.Xmlns + "soap", SoapNs),
                new XElement(XName.Get("Header", SoapNs)),
                new XElement(XName.Get("Body", SoapNs), body)
            );
        }
    }
}
