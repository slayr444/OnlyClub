namespace OnlyClub.Application.Ports;

public record PaymentResult(bool Success, string ProviderRef, string? Error);
