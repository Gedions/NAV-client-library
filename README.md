# NAV Client Library

This library provides generic OData V4 and SOAP client services for Microsoft Dynamics NAV / Business Central.

## Prerequisites

- .NET 6.0 or later
- A NAV/BC instance with OData V4 and SOAP web services enabled
- Valid credentials for basic or token authentication

## Configuration

Add the following sections to your `appsettings.json`:

```json
{
  "NavSoapService": {
    "Host": "http://example.com",
    "Port": "your soap port",
    "ServerInstance": "your server instance",
    "Company": "your company",
    "ObjectType": "Page",
    "ServiceType": "SOAP",
    "Username": "your username",
    "Password": "your password"
  },
  "NavODataService": {
    "Host": "http://example.com",
    "Port": "your ODataV4 port",
    "ServerInstance": "BC230",
    "Company": "your company",
    "Username": "your username",
    "Password": "your password"
  }
}
```

### Configuration Classes

#### NavSoapServiceConfig

```csharp
public class NavSoapServiceConfig
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string ServerInstance { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string ObjectType { get; set; } = string.Empty;
    public string ServiceType { get; set; } = string.Empty;

    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? BearerToken { get; set; }

    public Uri BaseUri
    {
        get
        {
            var typeSegment = string.IsNullOrWhiteSpace(ObjectType) ? "Page" : ObjectType;
            var path = $"{Host}:{Port}/{ServerInstance}/WS/{Company}/{typeSegment}/";
            return new Uri(path.StartsWith("http") ? path : $"http://{path}");
        }
    }
}
```

#### NavODataServiceConfig

```csharp
public class NavODataServiceConfig
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string ServerInstance { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;

    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? BearerToken { get; set; }

    public Uri BaseUri
    {
        get
        {
            var path = $"{Host}:{Port}/{ServerInstance}/ODataV4/Company('{Company}')/";
            return new Uri(path.StartsWith("http") ? path : $"http://{path}");
        }
    }
}
```

## Dependency Injection

In your `Program.cs` (or `Startup.cs`), register the services:

```csharp
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Options;
using Core.Interfaces;
using Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();

// Bind configuration
builder.Services.Configure<NavSoapServiceConfig>(
    builder.Configuration.GetSection("NavSoapService"));
builder.Services.Configure<NavODataServiceConfig>(
    builder.Configuration.GetSection("NavODataService"));

// Register factories
builder.Services.AddSingleton<INavSoapServiceFactory, NavSoapServiceFactory>();
builder.Services.AddSingleton<INavODataServiceFactory, NavODataServiceFactory>();

// Named HttpClient for SOAP
builder.Services.AddHttpClient("NavSoapClient", (sp, client) =>
{
    var cfg = sp.GetRequiredService<IOptions<NavSoapServiceConfig>>().Value;
    client.BaseAddress = cfg.BaseUri;
    client.DefaultRequestHeaders.Accept.Add(
        new MediaTypeWithQualityHeaderValue("application/json"));
    if (!string.IsNullOrWhiteSpace(cfg.BearerToken))
    {
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", cfg.BearerToken);
    }
    else if (!string.IsNullOrWhiteSpace(cfg.Username))
    {
        var cred = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{cfg.Username}:{cfg.Password}"));
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", cred);
    }
});

// Named HttpClient for OData
builder.Services.AddHttpClient("NavODataClient", (sp, client) =>
{
    var cfg = sp.GetRequiredService<IOptions<NavODataServiceConfig>>().Value;
    client.BaseAddress = cfg.BaseUri;
    client.DefaultRequestHeaders.Accept.Add(
        new MediaTypeWithQualityHeaderValue("application/json"));
    if (!string.IsNullOrWhiteSpace(cfg.BearerToken))
    {
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", cfg.BearerToken);
    }
    else if (!string.IsNullOrWhiteSpace(cfg.Username))
    {
        var cred = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{cfg.Username}:{cfg.Password}"));
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", cred);
    }
});

var app = builder.Build();
app.Run();
```

## Usage

Inject factories and call your services:

```csharp
public class MyAppService
{
    private readonly INavODataService<Customer> _customers;
    private readonly INavSoapService<Order> _orders;

    public MyAppService(
        INavODataServiceFactory odataFactory,
        INavSoapServiceFactory soapFactory)
    {
        _customers = odataFactory.Create<Customer>("Customers");
        _orders    = soapFactory.Create<Order>("SalesOrder");
    }

    public async Task Run()
    {
        var usCustomers = await _customers.GetEntitiesAsync("Country eq 'US'");
        var order       = await _orders.ReadAsync(
            new XElement("OrderId", "SO-1001"));
        // ...
    }
}
```

## Next Steps

- Add option validation (e.g. `IValidateOptions<T>`).  
- Use [dotnet user-secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) for sensitive data.  
- Configure timeouts or custom headers on `HttpClient`.  
- Prepare for NuGet packaging in your `.csproj`.

---
