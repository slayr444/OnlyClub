namespace OnlyClub.Application.Contract.Payments;

public record PaymentWebhookDto(
    Guid BookingId,
    string ProviderRef,
    decimal Amount,
    string Status);