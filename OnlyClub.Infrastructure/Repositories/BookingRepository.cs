using Microsoft.EntityFrameworkCore;
using OnlyClub.Application.Ports;
using OnlyClub.Core.Entities;
using OnlyClub.Core.Enums;

namespace OnlyClub.Infrastructure.Repositories;

public class BookingRepository(OnlyClubDbContext db) : IBookingRepository
{
    public Task<TableBooking?> GetAsync(Guid id, CancellationToken ct) =>
        db.Bookings
            .Include(b => b.EventTable).ThenInclude(t => t.Event)
            .Include(b => b.Payments)
            .FirstOrDefaultAsync(b => b.Id == id, ct);

    public Task<List<TableBooking>> GetByGuestAsync(Guid guestId, CancellationToken ct) =>
        db.Bookings
            .Include(b => b.EventTable).ThenInclude(t => t.Event)
            .Where(b => b.GuestId == guestId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(ct);

    public Task<List<TableBooking>> GetByEventAsync(Guid eventId, CancellationToken ct) =>
        db.Bookings
            .Include(b => b.EventTable).ThenInclude(t => t.Event)
            .Include(b => b.Payments)
            .Where(b => b.EventTable.EventId == eventId)
            .ToListAsync(ct);

    public Task<EventTable?> GetTableForUpdateAsync(Guid tableId, CancellationToken ct) =>
        db.EventTables
            .Include(t => t.Event)
            .FirstOrDefaultAsync(t => t.Id == tableId, ct);

    public Task<TableBooking?> GetForUpdateAsync(Guid id, CancellationToken ct) =>
        db.Bookings
            .Include(b => b.EventTable).ThenInclude(t => t.Event)
            .Include(b => b.Payments)
            .FirstOrDefaultAsync(b => b.Id == id, ct);

    public Task<TableBooking?> GetWithAggregateForUpdateAsync(Guid id, CancellationToken ct) =>
        db.Bookings
            .Include(b => b.EventTable).ThenInclude(t => t.Event)
            .Include(b => b.Payments)
            .FirstOrDefaultAsync(b => b.Id == id, ct);

    public Task<List<TableBooking>> GetExpiredHoldsForUpdateAsync(DateTime now, CancellationToken ct) =>
        db.Bookings
            .Include(b => b.EventTable).ThenInclude(t => t.Event)
            .Where(b => b.Status == BookingStatus.Held && b.HeldUntilAt < now)
            .OrderBy(b => b.HeldUntilAt)
            .Take(100)
            .ToListAsync(ct);

    public async Task<List<TableBooking>> GetConfirmedPastHoldUntilForUpdateAsync(DateTime now, CancellationToken ct)
    {
        var candidates = await db.Bookings
            .Include(b => b.EventTable).ThenInclude(t => t.Event)
            .Where(b => b.Status == BookingStatus.Confirmed
                        && b.EventTable.Event.StartsAt < now
                        && b.EventTable.Event.StartsAt > now.AddDays(-3))
            .ToListAsync(ct);

        return candidates
            .Where(b =>
            {
                var ev = b.EventTable.Event;

                var sameDayHold = ev.StartsAt.Date.Add(ev.HoldUntil);
                var actualHoldDateTime = sameDayHold > ev.StartsAt
                    ? sameDayHold
                    : ev.StartsAt.Date.AddDays(1).Add(ev.HoldUntil);

                return actualHoldDateTime < now;
            })
            .OrderBy(b => b.EventTable.Event.StartsAt)
            .Take(200)
            .ToList();        
    }
    public Task AddAsync(TableBooking booking, CancellationToken ct) =>
        db.Bookings.AddAsync(booking, ct).AsTask();

    public Task AddPaymentAsync(Payment payment, CancellationToken ct) =>
        db.Payments.AddAsync(payment, ct).AsTask();

    public Task<Payment?> GetPaymentByProviderRefForUpdateAsync(string providerRef, CancellationToken ct) =>
        db.Payments
            .Include(p => p.Booking)
            .FirstOrDefaultAsync(p => p.ProviderRef == providerRef, ct);

    public Task<Payment?> GetPendingPaymentAsync(Guid bookingId, CancellationToken ct) =>
        db.Payments
            .Where(p => p.BookingId == bookingId && p.Status == PaymentStatus.Pending)
            .OrderByDescending(p => p.Id)
            .FirstOrDefaultAsync(ct);
}