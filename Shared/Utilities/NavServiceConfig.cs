namespace Shared.Utilities
{
    public class NavServiceConfig
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
        public string ServerInstance { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public string ObjectType { get; set; } = string.Empty;
        public string ServiceType { get; set; } = string.Empty;

        // Auth
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? BearerToken { get; set; }

        public Uri BaseUri => new(ServiceType.ToUpperInvariant() switch
        {
            "ODATAV4" => $"{Host}:{Port}/{ServerInstance}/ODataV4/Company('{Company}')/",
            "SOAP" => $"{Host}:{Port}/{ServerInstance}/WS/{Company}/" +
                      $"{(string.IsNullOrWhiteSpace(ObjectType) ? "Page" : ObjectType)}/",
            _ => throw new NotImplementedException($"Unknown service type: {ServiceType}")
        });
    }
}
