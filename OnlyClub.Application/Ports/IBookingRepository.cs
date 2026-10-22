using OnlyClub.Core.Entities;

namespace OnlyClub.Application.Ports;

public interface IBookingRepository
{
    Task<EventTable?> GetTableForUpdateAsync(Guid id, CancellationToken ct);
    Task<TableBooking?> GetForUpdateAsync(Guid id, CancellationToken ct);
    Task<TableBooking?> GetWithAggregateForUpdateAsync(Guid id, CancellationToken ct);
    Task<TableBooking?> GetAsync(Guid id, CancellationToken ct);
    Task<List<TableBooking>> GetByGuestAsync(Guid guestId, CancellationToken ct);

    Task<List<TableBooking>> GetByEventAsync(Guid eventId, CancellationToken ct);   

    Task<List<TableBooking>> GetExpiredHoldsForUpdateAsync(DateTime now, CancellationToken ct);
    Task<List<TableBooking>> GetConfirmedPastHoldUntilForUpdateAsync(DateTime now, CancellationToken ct);

    Task AddAsync(TableBooking booking, CancellationToken ct);
    Task AddPaymentAsync(Payment payment, CancellationToken ct);

    Task<Payment?> GetPaymentByProviderRefForUpdateAsync(string providerRef, CancellationToken ct);
    Task<Payment?> GetPendingPaymentAsync(Guid bookingId, CancellationToken ct);
}