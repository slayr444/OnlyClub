namespace OnlyClub.Application.Contract.Bookings;

public record BookingDto(
    Guid Id,
    Guid EventTableId,
    string TableNumber,
    int PartySize,
    string Status,
    decimal DepositPaid,
    decimal RemainingMinSpend,
    string? QrToken,
    DateTime? HeldUntilAt); 

public record InitiatePaymentResult(string? ProviderRef, bool Success);
public record PaymentWebhookRequest(string ProviderRef, string Status); 