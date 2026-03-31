namespace ApiGateway.Api.Http;

public sealed class ServiceUrlsOptions
{
    public string Product { get; set; } = "";
    public string Customer { get; set; } = "";
    public string Order { get; set; } = "";
    public string Notification { get; set; } = "";
}