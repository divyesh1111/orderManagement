using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using ApiGateway.Api.Dtos;
using ApiGateway.Api.Http;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.Configure<ServiceUrlsOptions>(builder.Configuration.GetSection("ServiceUrls"));

builder.Services.AddHttpClient<OrderClient>((sp, client) =>
{
    var urls = sp.GetRequiredService<IOptions<ServiceUrlsOptions>>().Value;
    client.BaseAddress = new Uri(urls.Order);
});

builder.Services.AddHttpClient<CustomerClient>((sp, client) =>
{
    var urls = sp.GetRequiredService<IOptions<ServiceUrlsOptions>>().Value;
    client.BaseAddress = new Uri(urls.Customer);
});

builder.Services.AddHttpClient<ProductClient>((sp, client) =>
{
    var urls = sp.GetRequiredService<IOptions<ServiceUrlsOptions>>().Value;
    client.BaseAddress = new Uri(urls.Product);
});

builder.Services.AddHttpClient("product-swagger", (sp, client) =>
{
    var urls = sp.GetRequiredService<IOptions<ServiceUrlsOptions>>().Value;
    client.BaseAddress = new Uri(urls.Product);
    client.Timeout = TimeSpan.FromSeconds(5);
});

builder.Services.AddHttpClient("customer-swagger", (sp, client) =>
{
    var urls = sp.GetRequiredService<IOptions<ServiceUrlsOptions>>().Value;
    client.BaseAddress = new Uri(urls.Customer);
    client.Timeout = TimeSpan.FromSeconds(5);
});

builder.Services.AddHttpClient("order-swagger", (sp, client) =>
{
    var urls = sp.GetRequiredService<IOptions<ServiceUrlsOptions>>().Value;
    client.BaseAddress = new Uri(urls.Order);
    client.Timeout = TimeSpan.FromSeconds(5);
});

builder.Services.AddHttpClient("notification-swagger", (sp, client) =>
{
    var urls = sp.GetRequiredService<IOptions<ServiceUrlsOptions>>().Value;
    client.BaseAddress = new Uri(urls.Notification);
    client.Timeout = TimeSpan.FromSeconds(5);
});

var app = builder.Build();

app.UseSwagger();

app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "ApiGateway");
    c.SwaggerEndpoint("/proxy-swagger/product/swagger.json", "ProductService");
    c.SwaggerEndpoint("/proxy-swagger/customer/swagger.json", "CustomerService");
    c.SwaggerEndpoint("/proxy-swagger/order/swagger.json", "OrderService");
    c.SwaggerEndpoint("/proxy-swagger/notification/swagger.json", "NotificationService");
});

static string RewriteServersOnly(string json, string baseUrl)
{
    var node = JsonNode.Parse(json) as JsonObject;
    if (node is null) return json;

    node["servers"] = new JsonArray(new JsonObject { ["url"] = baseUrl });

    return node.ToJsonString(new JsonSerializerOptions(JsonSerializerDefaults.Web));
}

static async Task<IResult> ProxySwaggerAsync(IHttpClientFactory factory, string clientName, HttpRequest req, CancellationToken ct)
{
    try
    {
        var http = factory.CreateClient(clientName);
        using var res = await http.GetAsync("/swagger/v1/swagger.json", ct);

        if (!res.IsSuccessStatusCode)
            return Results.Problem(statusCode: 502, title: "DownstreamSwaggerUnavailable", detail: $"Status {(int)res.StatusCode}");

        var json = await res.Content.ReadAsStringAsync(ct);
        var baseUrl = $"{req.Scheme}://{req.Host}";
        var rewritten = RewriteServersOnly(json, baseUrl);

        return Results.Text(rewritten, "application/json");
    }
    catch (TaskCanceledException)
    {
        return Results.Problem(statusCode: 504, title: "DownstreamSwaggerTimeout");
    }
    catch (Exception ex)
    {
        return Results.Problem(statusCode: 502, title: "DownstreamSwaggerError", detail: ex.Message);
    }
}

app.MapGet("/proxy-swagger/product/swagger.json", (IHttpClientFactory f, HttpRequest r, CancellationToken ct) =>
    ProxySwaggerAsync(f, "product-swagger", r, ct)).ExcludeFromDescription();

app.MapGet("/proxy-swagger/customer/swagger.json", (IHttpClientFactory f, HttpRequest r, CancellationToken ct) =>
    ProxySwaggerAsync(f, "customer-swagger", r, ct)).ExcludeFromDescription();

app.MapGet("/proxy-swagger/order/swagger.json", (IHttpClientFactory f, HttpRequest r, CancellationToken ct) =>
    ProxySwaggerAsync(f, "order-swagger", r, ct)).ExcludeFromDescription();

app.MapGet("/proxy-swagger/notification/swagger.json", (IHttpClientFactory f, HttpRequest r, CancellationToken ct) =>
    ProxySwaggerAsync(f, "notification-swagger", r, ct)).ExcludeFromDescription();

app.MapGet("/api/orders/{id:guid}/details", async (
    Guid id,
    OrderClient orders,
    CustomerClient customers,
    ProductClient products,
    CancellationToken ct) =>
{
    var order = await orders.GetOrderAsync(id, ct);
    if (order is null) return Results.NotFound();

    var customer = await customers.GetCustomerAsync(order.CustomerId, ct);
    if (customer is null) return Results.Problem(statusCode: 502, title: "CustomerServiceUnavailable");

    var productTasks = order.Items
        .Select(i => i.ProductId)
        .Distinct()
        .ToDictionary(pid => pid, pid => products.GetProductAsync(pid, ct));

    await Task.WhenAll(productTasks.Values);

    var productMap = productTasks.ToDictionary(kv => kv.Key, kv => kv.Value.Result);

    if (productMap.Values.Any(p => p is null)) return Results.Problem(statusCode: 502, title: "ProductServiceUnavailable");

    var items = order.Items.Select(i =>
    {
        var p = productMap[i.ProductId]!;
        var productDto = new OrderDetailsProductDto(p.Id, p.Name, p.Price);
        return new OrderDetailsItemDto(productDto, i.Quantity, i.UnitPriceSnapshot);
    }).ToList();

    var customerDto = new OrderDetailsCustomerDto(customer.Id, customer.FirstName, customer.LastName, customer.Email);
    var dto = new OrderDetailsDto(order.Id, order.CreatedAtUtc, order.Status, customerDto, items);

    return Results.Ok(dto);
});

app.MapReverseProxy();

app.Run();