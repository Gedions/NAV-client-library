using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace Shared.Models
{
    public abstract class BaseSoapODataModel
    {
        [JsonPropertyName("@odata.etag")]
        [XmlIgnore]
        public string? ETag { get; set; }

        [XmlElement("Key")]
        [JsonIgnore]
        public string? Key { get; set; }
    }
}
