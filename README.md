# NAV Client Library

This library provides generic OData V4 and SOAP client services for Microsoft Dynamics NAV / Dynamics 365 Business Central.

## Installation
Using .NET CLI

`dotnet add package Og.Nav.Client.Library --version 1.0.4`

`dotnet add package Og.Nav.Core --version 1.0.0`

`dotnet add package Og.Nav.Shared --version 1.0.0`

Alternatively you can use Manage Nuget Packages from Visual Studio IDE

## Prerequisites

- .NET 8.0 or later
- A NAV/BC instance with OData V4 and/or SOAP web services enabled
- Valid credentials for basic or token authentication

## Configuration

Add the following sections to your `appsettings.json`

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

You can store sensitive information such as username and password using secrets.

If your NAV/ D365BC uses windows authentication you can ignore the username and password section.

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

// Bind configuration
builder.Services.Configure<NavSoapServiceConfig>(
    builder.Configuration.GetSection("NavSoapService"));
builder.Services.Configure<NavODataServiceConfig>(
    builder.Configuration.GetSection("NavODataService"));

// Register factories
builder.Services.AddSingleton<INavSoapServiceFactory, NavSoapServiceFactory>(); // For working with SOAP web service
builder.Services.AddSingleton<INavODataServiceFactory, NavODataServiceFactory>(); // For working with ODataV4

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

// For Windows Authentication on NAV/ D365BC
builder.Services.AddHttpClient("NavSoapClient", (sp, client) =>
{
    var config = sp.GetRequiredService<IOptions<NavSoapServiceConfig>>().Value;
    client.BaseAddress = config.BaseUri;
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
}).ConfigurePrimaryHttpMessageHandler(() =>
    {
        var handler = new HttpClientHandler();
        handler.UseDefaultCredentials = true;
        return handler;
    });

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
        var usCustomers = await _customers.GetEntitiesAsync("Country eq 'US'"); // example using ODataV4 web services
        var order       = await _orders.ReadAsync(
            new XElement("OrderId", "SO-1001")); // example using SOAP web service
    }
}
```
