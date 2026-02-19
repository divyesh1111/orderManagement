using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OrderService.Api.Data;
using OrderService.Api.Endpoints;
using OrderService.Api.Http;
using OrderService.Api.Messaging;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<OrderDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.Configure<ServiceUrlsOptions>(builder.Configuration.GetSection("ServiceUrls"));
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMq"));

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

builder.Services.AddSingleton<OrderEventPublisher>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    await db.Database.MigrateAsync();
}

app.UseSwagger();
app.UseSwaggerUI();

app.MapOrderEndpoints();

app.Run();
