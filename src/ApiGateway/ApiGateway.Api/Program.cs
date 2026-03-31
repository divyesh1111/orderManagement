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

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "ApiGateway"));

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