using BlazorFrontend.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5000";

builder.Services.AddHttpClient<ProductsApi>(c => c.BaseAddress = new Uri(apiBaseUrl));
builder.Services.AddHttpClient<CustomersApi>(c => c.BaseAddress = new Uri(apiBaseUrl));
builder.Services.AddHttpClient<OrdersApi>(c => c.BaseAddress = new Uri(apiBaseUrl));
builder.Services.AddHttpClient<NotificationsApi>(c => c.BaseAddress = new Uri(apiBaseUrl));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();