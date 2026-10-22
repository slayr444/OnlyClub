using Microsoft.Extensions.Logging;
using OnlyClub.Application.Contract.Bookings;
using OnlyClub.Application.Ports;
using OnlyClub.Core;
using OnlyClub.Core.Common;
using OnlyClub.Core.Entities;
using OnlyClub.Core.Enums;

namespace OnlyClub.Application.Services;

public sealed class BookingService(
    IBookingRepository bookings,
    IPaymentProvider payments,
    IQrSigner qr,
    IAuditRepository audit,
    IScheduledMessageRepository scheduled,
    IUnitOfWork uow,
    ILogger<BookingService> log)
{
    private static readonly TimeSpan HoldDuration = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan UnpaidReminderLead = TimeSpan.FromMinutes(5);

    public async Task<BookingDto> CreateAsync(Guid guestId, CreateBookingRequest req, CancellationToken ct)
    {
        if (req.PartySize < 1)
            throw new DomainException("Invalid party size");

        await uow.BeginTransactionAsync(ct);
        try
        {
            var table = await bookings.GetTableForUpdateAsync(req.EventTableId, ct)
                        ?? throw new NotFoundException("Table");
            if (table.Status != TableStatus.Free)
                throw new DomainException("Table not free");
            if (req.PartySize > table.Capacity)
                throw new DomainException("Party exceeds capacity");

            var booking = new TableBooking
            {
                EventTableId = table.Id,
                GuestId = guestId,
                PartySize = req.PartySize,
                HeldUntilAt = DateTime.UtcNow.Add(HoldDuration),
            };
            table.Status = TableStatus.Held;
            await bookings.AddAsync(booking, ct);

            await scheduled.AddAsync(new ScheduledMessage
            {
                Type = ScheduledMessageType.UnpaidHoldReminder,
                GuestId = guestId,
                RelatedId = booking.Id,
                ScheduledAt = booking.HeldUntilAt - UnpaidReminderLead,
            }, ct);

            await uow.SaveAsync(ct);
            await uow.CommitAsync(ct);

            log.LogInformation(
                "Booking {BookingId} created: table {TableId}, guest {GuestId}, party {PartySize}, hold until {HeldUntil:o}",
                booking.Id, table.Id, guestId, req.PartySize, booking.HeldUntilAt);

            return Map(booking, table);
        }
        catch
        {
            await uow.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<InitiatePaymentResult> InitiatePaymentAsync(
        Guid bookingId, Guid guestId, CancellationToken ct)
    {
        await uow.BeginTransactionAsync(ct);
        try
        {
            var booking = await bookings.GetForUpdateAsync(bookingId, ct)
                          ?? throw new NotFoundException("Booking");
            if (booking.GuestId != guestId)
                throw new ForbiddenException("Not your booking");
            if (booking.Status != BookingStatus.Held)
                throw new DomainException($"Booking is {booking.Status}, cannot pay");
            if (DateTime.UtcNow >= booking.HeldUntilAt)
                throw new DomainException("Hold expired");

            var existing = await bookings.GetPendingPaymentAsync(booking.Id, ct);
            if (existing is not null)
            {
                await uow.RollbackAsync(CancellationToken.None);
                return new InitiatePaymentResult(existing.ProviderRef, true);
            }

            var idempotencyKey = $"booking-{booking.Id:N}";
            var charge = await payments.ChargeAsync(booking.EventTable.Deposit, idempotencyKey, ct);
            if (!charge.Success)
            {
                await uow.RollbackAsync(CancellationToken.None);
                log.LogWarning("Charge failed for booking {BookingId}: {Error}",
                    booking.Id, charge.Error);
                return new InitiatePaymentResult(null, false);
            }

            var payment = new Payment
            {
                BookingId = booking.Id,
                Amount = booking.EventTable.Deposit,
                ProviderRef = charge.ProviderRef!,
                Status = PaymentStatus.Pending,
            };
            await bookings.AddPaymentAsync(payment, ct);
            await uow.SaveAsync(ct);
            await uow.CommitAsync(ct);

            log.LogInformation("Payment {ProviderRef} initiated for booking {BookingId} amount {Amount}",
                payment.ProviderRef, booking.Id, payment.Amount);

            return new InitiatePaymentResult(payment.ProviderRef, true);
        }
        catch
        {
            await uow.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task HandlePaymentWebhookAsync(PaymentWebhookRequest webhook, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(webhook.ProviderRef);

        await uow.BeginTransactionAsync(ct);
        try
        {
            var payment = await bookings.GetPaymentByProviderRefForUpdateAsync(webhook.ProviderRef, ct)
                          ?? throw new NotFoundException("Payment");

            if (payment.Status is PaymentStatus.Succeeded
                              or PaymentStatus.Refunded
                              or PaymentStatus.RefundRequired
                              or PaymentStatus.Cancelled)
            {
                await uow.RollbackAsync(CancellationToken.None);
                log.LogInformation("Payment {ProviderRef} already handled (status {Status})",
                    webhook.ProviderRef, payment.Status);
                return;
            }

            var booking = await bookings.GetWithAggregateForUpdateAsync(payment.BookingId, ct)
                          ?? throw new NotFoundException("Booking");

            if (webhook.Status == "failed")
            {
                payment.Status = PaymentStatus.Failed;
                await uow.SaveAsync(ct);
                await uow.CommitAsync(ct);
                log.LogWarning("Payment {ProviderRef} failed for booking {BookingId}",
                    webhook.ProviderRef, booking.Id);
                return;
            }

            var isOnTime = booking.Status == BookingStatus.Held
                           && DateTime.UtcNow < booking.HeldUntilAt;

            if (!isOnTime)
            {
                payment.Status = PaymentStatus.RefundRequired;
                await uow.SaveAsync(ct);
                await uow.CommitAsync(ct);
                log.LogWarning(
                    "Late payment {ProviderRef} for booking {BookingId} (status {Status}) — refund required",
                    webhook.ProviderRef, booking.Id, booking.Status);
                return;
            }

            payment.Status = PaymentStatus.Succeeded;
            booking.DepositPaid = booking.EventTable.Deposit;
            booking.Confirm(qr.Sign(booking.Id, booking.EventTable.EventId));

            await scheduled.CancelRelatedAsync(booking.Id, ct);
            await scheduled.AddAsync(new ScheduledMessage
            {
                Type = ScheduledMessageType.BookingConfirmation,
                GuestId = booking.GuestId,
                RelatedId = booking.Id,
                ScheduledAt = DateTime.UtcNow,
            }, ct);

            await uow.SaveAsync(ct);
            await uow.CommitAsync(ct);

            log.LogInformation("Payment {ProviderRef} confirmed booking {BookingId}",
                webhook.ProviderRef, booking.Id);
        }
        catch
        {
            await uow.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<CancelBookingResult> CancelAsync(
        Guid bookingId, Guid actorId, bool isManager, CancellationToken ct)
    {
        await uow.BeginTransactionAsync(ct);
        try
        {
            var booking = await bookings.GetWithAggregateForUpdateAsync(bookingId, ct)
                          ?? throw new NotFoundException("Booking");
            if (booking.GuestId != actorId && !isManager)
                throw new ForbiddenException("Not your booking");
            if (booking.Status is BookingStatus.Cancelled or BookingStatus.NoShow)
                throw new DomainException($"Booking already {booking.Status}");

            var refund = ComputeRefund(booking, isManager);
            booking.Cancel();         

            await scheduled.CancelRelatedAsync(booking.Id, ct);

            foreach (var p in booking.Payments.Where(p => p.Status == PaymentStatus.Succeeded).ToList())
            {
                if (refund > 0)
                {
                    var rr = await payments.RefundAsync(
                        p.ProviderRef!, refund, $"refund-{booking.Id:N}", ct);
                    p.Status = rr.Success ? PaymentStatus.Refunded : PaymentStatus.RefundFailed;
                    if (!rr.Success)
                        log.LogError("Refund failed for payment {ProviderRef}: {Error}",
                            p.ProviderRef, rr.Error);
                }
                else
                {
                    p.Status = PaymentStatus.Refunded;
                }
            }
            foreach (var p in booking.Payments.Where(p => p.Status == PaymentStatus.Pending).ToList())
                p.Status = PaymentStatus.Cancelled;

            if (isManager)
            {
                await audit.AddAsync(new AuditLog
                {
                    ActorId = actorId,
                    Action = AuditAction.BookingCancel,
                    EntityType = nameof(TableBooking),
                    EntityId = booking.Id,
                }, ct);
            }

            await uow.SaveAsync(ct);
            await uow.CommitAsync(ct);

            log.LogInformation(
                "Booking {BookingId} cancelled by {ActorId} (manager={IsManager}); refund {Refund}",
                booking.Id, actorId, isManager, refund);

            return new CancelBookingResult(refund);
        }
        catch
        {
            await uow.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task SeatAsync(Guid bookingId, Guid guardId, CancellationToken ct)
    {
        await uow.BeginTransactionAsync(ct);
        try
        {
            var booking = await bookings.GetWithAggregateForUpdateAsync(bookingId, ct)
                          ?? throw new NotFoundException("Booking");

            if (booking.Status == BookingStatus.Seated)
            {
                await uow.RollbackAsync(CancellationToken.None);
                return;        
            }
            if (booking.Status != BookingStatus.Confirmed)
                throw new DomainException($"Cannot seat booking in status {booking.Status}");

            booking.Seat();         

            await audit.AddAsync(new AuditLog
            {
                ActorId = guardId,
                Action = AuditAction.BookingSeat,
                EntityType = nameof(TableBooking),
                EntityId = booking.Id,
            }, ct);

            await uow.SaveAsync(ct);
            await uow.CommitAsync(ct);

            log.LogInformation("Booking {BookingId} seated by guard {GuardId}", booking.Id, guardId);
        }
        catch
        {
            await uow.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<List<BookingDto>> MyTicketsAsync(Guid guestId, CancellationToken ct)
    {
        var list = await bookings.GetByGuestAsync(guestId, ct);
        return list.Select(b => Map(b, b.EventTable)).ToList();
    }

    public async Task<int> ReleaseExpiredHoldsAsync(CancellationToken ct)
    {
        await uow.BeginTransactionAsync(ct);
        try
        {
            var expired = await bookings.GetExpiredHoldsForUpdateAsync(DateTime.UtcNow, ct);
            if (expired.Count == 0)
            {
                await uow.RollbackAsync(CancellationToken.None);
                return 0;
            }

            foreach (var b in expired)
            {
                b.Cancel();          

                await scheduled.CancelRelatedAsync(b.Id, ct);
                await scheduled.AddAsync(new ScheduledMessage
                {
                    Type = ScheduledMessageType.HoldExpired,
                    GuestId = b.GuestId,
                    RelatedId = b.Id,
                    ScheduledAt = DateTime.UtcNow,
                }, ct);
            }

            await uow.SaveAsync(ct);
            await uow.CommitAsync(ct);

            log.LogInformation("Released {Count} expired holds", expired.Count);
            return expired.Count;
        }
        catch
        {
            await uow.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<int> MarkNoShowsAsync(CancellationToken ct)
    {
        await uow.BeginTransactionAsync(ct);
        try
        {
            var candidates = await bookings.GetConfirmedPastHoldUntilForUpdateAsync(DateTime.UtcNow, ct);
            if (candidates.Count == 0)
            {
                await uow.RollbackAsync(CancellationToken.None);
                return 0;
            }

            foreach (var b in candidates)
            {
                b.MarkNoShow();         
                await scheduled.CancelRelatedAsync(b.Id, ct);
            }

            await uow.SaveAsync(ct);
            await uow.CommitAsync(ct);

            log.LogInformation("Marked {Count} no-shows", candidates.Count);
            return candidates.Count;
        }
        catch
        {
            await uow.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private static decimal ComputeRefund(TableBooking booking, bool isManager)
    {
        if (booking.DepositPaid <= 0) return 0m;
        if (isManager) return booking.DepositPaid;
        return booking.RefundAmount(booking.EventTable.Event.StartsAt, DateTime.UtcNow);
    }

    private static BookingDto Map(TableBooking b, EventTable t)
    {
        var remainingMinSpend = Math.Max(0m, t.MinSpend - b.DepositPaid);
        return new BookingDto(
            b.Id,                                        
            b.EventTableId,                              
            t.Number.ToString(),                             
            b.PartySize,                                 
            b.Status.ToString(),                         
            b.DepositPaid,                               
            remainingMinSpend,                           
            b.QrToken,                                   
            b.Status == BookingStatus.Held ? b.HeldUntilAt : null);   
    }
}