namespace BlazorFrontend.Models;

public sealed record NotificationReadDto(Guid Id, Guid OrderId, string Message, DateTime CreatedAtUtc);