using OnlyClub.Application.Ports;
using OnlyClub.Core;
using OnlyClub.Core.Common;
using OnlyClub.Core.Entities;
using OnlyClub.Core.Enums;

namespace OnlyClub.Application.Services;

public class CommissionService(
    ICommissionRepository commissions, IEventRepository events, IBookingRepository bookings,
    IGuestListRepository entries, IPromoterRepository promoters, IAuditRepository audit, IUnitOfWork uow)
{
    private const decimal PerCheckIn = 300m;
    private const decimal VolumeBonus = 2000m;
    private const int VolumeThreshold = 20;
    private const decimal DepositRate = 0.05m;

    public async Task CloseAsync(Guid eventId, Guid managerId, CancellationToken ct)
    {
        var ev = await events.GetAsync(eventId, ct) ?? throw new NotFoundException("Event");
        if (ev.IsClosed) throw new DomainException("Already closed");
        ev.IsClosed = true; ev.ClosedAt = DateTime.UtcNow;
        await RecalcAsync(ev, ct);
        await audit.AddAsync(new AuditLog { ActorId = managerId, Action = AuditAction.EventClose, EntityType = "Event", EntityId = eventId }, ct);
        await uow.SaveAsync(ct);
    }

    public async Task ReopenAsync(Guid eventId, string reason, Guid managerId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new DomainException("Reason required");
        var ev = await events.GetAsync(eventId, ct) ?? throw new NotFoundException("Event");
        ev.IsClosed = false; ev.ClosedAt = null;
        await RecalcAsync(ev, ct);
        await audit.AddAsync(new AuditLog { ActorId = managerId, Action = AuditAction.EventReopen, EntityType = "Event", EntityId = eventId, Reason = reason }, ct);
        await uow.SaveAsync(ct);
    }

    private async Task RecalcAsync(Event ev, CancellationToken ct)
    {
        var windowStart = ev.StartsAt.AddHours(-72);
        var linesByPromoter = new Dictionary<Guid, List<CommissionLine>>();

        var eventEntries = await entries.ForEventAsync(ev.Id, ct);
        var checkedInByPromoter = new Dictionary<Guid, int>();
        foreach (var e in eventEntries.Where(x => x.Status == GuestListStatus.CheckedIn))
        {
            var touch = await promoters.GetFirstTouchAsync(e.GuestId, windowStart, ev.StartsAt, ct);
            if (touch is null) continue;
            GetBucket(linesByPromoter, touch.PromoterId).Add(new CommissionLine { GuestId = e.GuestId, Rule = "PerCheckIn", Amount = PerCheckIn });
            checkedInByPromoter[touch.PromoterId] = checkedInByPromoter.GetValueOrDefault(touch.PromoterId) + 1;
        }

        foreach (var (pid, count) in checkedInByPromoter)
            if (count >= VolumeThreshold)
                GetBucket(linesByPromoter, pid).Add(new CommissionLine { Rule = "VolumeBonus", Amount = VolumeBonus });

        var eventBookings = await bookings.GetByEventAsync(ev.Id, ct);
        foreach (var b in eventBookings.Where(x => x.Status is BookingStatus.Confirmed or BookingStatus.Seated))
        {
            var touch = await promoters.GetFirstTouchAsync(b.GuestId, windowStart, ev.StartsAt, ct);
            if (touch is null) continue;
            GetBucket(linesByPromoter, touch.PromoterId).Add(new CommissionLine
            {
                GuestId = b.GuestId,
                Rule = "DepositShare",
                Amount = Math.Round(b.DepositPaid * DepositRate, 2)
            });
        }

        foreach (var (promoterId, lines) in linesByPromoter)
        {
            var commission = await commissions.GetForPromoterEventAsync(promoterId, ev.Id, ct);
            if (commission is null) { commission = new Commission { EventId = ev.Id, PromoterId = promoterId }; await commissions.AddAsync(commission, ct); }
            if (commission.Status == CommissionStatus.Paid) continue;
            commission.Status = CommissionStatus.Pending;
            commission.Recalculate(lines);
        }
    }

    static List<CommissionLine> GetBucket(Dictionary<Guid, List<CommissionLine>> d, Guid k)
        => d.TryGetValue(k, out var b) ? b : d[k] = [];

    public async Task ApproveAsync(Guid commissionId, Guid managerId, CancellationToken ct)
    {
        var c = await commissions.GetAsync(commissionId, ct) ?? throw new NotFoundException("Commission");
        c.Approve();
        await audit.AddAsync(new AuditLog { ActorId = managerId, Action = AuditAction.CommissionApprove, EntityType = "Commission", EntityId = c.Id }, ct);
        await uow.SaveAsync(ct);
    }

    public async Task PayAsync(Guid commissionId, Guid managerId, CancellationToken ct)
    {
        var c = await commissions.GetAsync(commissionId, ct) ?? throw new NotFoundException("Commission");
        c.Pay();
        await audit.AddAsync(new AuditLog { ActorId = managerId, Action = AuditAction.CommissionPay, EntityType = "Commission", EntityId = c.Id }, ct);
        await uow.SaveAsync(ct);
    }
}