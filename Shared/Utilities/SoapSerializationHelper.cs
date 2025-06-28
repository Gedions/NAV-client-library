using System.Text;
using System.Xml.Serialization;

namespace Shared.Utilities
{
    public static class SoapSerializationHelper
    {
        public static string SerializeEntity<T>(string wrapperName, T entity, string serviceNamespace) where T : class
        {
            var xmlSerializer = new XmlSerializer(typeof(T), new XmlRootAttribute(wrapperName)
            {
                Namespace = $"urn:microsoft-dynamics-schemas/page/{serviceNamespace}"
            });

            using var stringWriter = new Utf8StringWriter();
            xmlSerializer.Serialize(stringWriter, entity);
            return stringWriter.ToString();
        }

        private class Utf8StringWriter : StringWriter
        {
            public override Encoding Encoding => Encoding.UTF8;
        }
    }

}
