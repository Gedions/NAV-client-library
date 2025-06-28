using Shared.Models;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Shared.Utilities
{
    public static class SoapResponseParser
    {
        public static List<T> ParseReadMultiple<T>(string soapResponse) where T : class
        {
            var xdoc = XDocument.Parse(soapResponse);

            // Namespaces
            XNamespace soapNs = "http://schemas.xmlsoap.org/soap/envelope/";
            XNamespace dataNs = $"urn:microsoft-dynamics-schemas/page/{typeof(T).Name.ToLower()}";

            // Navigate to the nested ReadMultiple_Result that contains the items
            var readMultipleResult = xdoc
                .Descendants(soapNs + "Body")
                .Descendants(dataNs + "ReadMultiple_Result")
                .FirstOrDefault()?
                .Element(dataNs + "ReadMultiple_Result");

            if (readMultipleResult == null)
                return new List<T>(); // no data found

            var items = readMultipleResult.Elements(dataNs + typeof(T).Name);

            var serializer = new XmlSerializer(typeof(T), new XmlRootAttribute(typeof(T).Name) { Namespace = dataNs.NamespaceName });

            var result = new List<T>();

            foreach (var item in items)
            {
                using var reader = item.CreateReader();
                if (serializer.Deserialize(reader) is T obj)
                    result.Add(obj);
            }

            return result;
        }

        public static T? ParseRead<T>(string soapResponse) where T : class
        {
            var xdoc = XDocument.Parse(soapResponse);

            // Namespaces
            XNamespace soapNs = "http://schemas.xmlsoap.org/soap/envelope/";
            XNamespace dataNs = $"urn:microsoft-dynamics-schemas/page/{typeof(T).Name.ToLower()}";

            var readResult = xdoc
                .Descendants(soapNs + "Body")
                .Descendants(dataNs + "Read_Result")
                .FirstOrDefault();

            var entityElement = readResult?.Element(dataNs + typeof(T).Name);
            if (entityElement == null)
                return null;

            using var reader = entityElement.CreateReader();
            var serializer = new XmlSerializer(typeof(T), new XmlRootAttribute
            {
                ElementName = typeof(T).Name,
                Namespace = dataNs.NamespaceName
            });

            return serializer.Deserialize(reader) as T;
        }

        public static SoapResult ParseReadCodeunit(string soapResponse, string methodName, string codeunitName)
        {
            try
            {
                var xdoc = XDocument.Parse(soapResponse);

                XNamespace soapNs = "http://schemas.xmlsoap.org/soap/envelope/";
                XNamespace dataNs = $"urn:microsoft-dynamics-schemas/codeunit/{codeunitName}";

                var readResult = xdoc
                    .Descendants(soapNs + "Body")
                    .Descendants(dataNs + $"{methodName}_Result")
                    .FirstOrDefault();

                var returnValueElement = readResult?.Element(dataNs + "return_value");
                if (returnValueElement == null)
                {
                    return new SoapResult
                    {
                        Success = false,
                        Message = "return_value element not found."
                    };
                }

                var value = returnValueElement.Value;

                return new SoapResult
                {
                    Success = true,
                    Message = "Success",
                    ReturnValue = value
                };
            }
            catch (Exception ex)
            {
                return new SoapResult
                {
                    Success = false,
                    Message = $"Error parsing response: {ex.Message}"
                };
            }
        }

        public static T? ParseCreateOrUpdate<T>(string soapResponse) where T : class
        {
            return ParseResultByTag<T>(soapResponse, "Create_Result") ??
                   ParseResultByTag<T>(soapResponse, "Update_Result");
        }

        public static bool ParseDelete(string soapResponse)
        {
            return soapResponse.Contains("Delete_Result");
        }

        private static T? ParseResultByTag<T>(string soapResponse, string tagName) where T : class
        {
            var xdoc = XDocument.Parse(soapResponse);

            XNamespace soapNs = "http://schemas.xmlsoap.org/soap/envelope/";
            XNamespace dataNs = $"urn:microsoft-dynamics-schemas/page/{typeof(T).Name.ToLower()}";

            var resultElement = xdoc
                .Descendants(soapNs + "Body")
                .Descendants(dataNs + tagName)
                .FirstOrDefault();

            var entityElement = resultElement?.Element(dataNs + typeof(T).Name);
            if (entityElement == null)
                return null;

            using var reader = entityElement.CreateReader();
            var serializer = new XmlSerializer(typeof(T), new XmlRootAttribute
            {
                ElementName = typeof(T).Name,
                Namespace = dataNs.NamespaceName
            });

            return serializer.Deserialize(reader) as T;
        }
    }
}
