using OnlyClub.Application.Contract.Events;
using OnlyClub.Application.Contract.Promoters;
using OnlyClub.Application.Ports;
using OnlyClub.Core;
using OnlyClub.Core.Common;
using OnlyClub.Core.Entities;
using OnlyClub.Core.Enums;

namespace OnlyClub.Application.Services;

public class EventService(IEventRepository events, IUnitOfWork uow)
{
    public async Task<EventDto> CreateAsync(CreateEventRequest req, CancellationToken ct)
    {
        var ev = new Event { Title = req.Title, StartsAt = req.StartsAt, GuestListLimit = req.GuestListLimit, MinAge = req.MinAge };
        foreach (var t in req.Tables) ev.Tables.Add(new EventTable { Number = t.Number, Capacity = t.Capacity, MinSpend = t.MinSpend, Deposit = t.Deposit });
        await events.AddAsync(ev, ct); await uow.SaveAsync(ct);
        return Map(ev);
    }
    public async Task<List<EventDto>> ListAsync(CancellationToken ct) => (await events.ListAsync(ct)).Select(Map).ToList();
    public async Task<List<TableDto>> TablesAsync(Guid eventId, CancellationToken ct)
    {
        var ev = await events.GetAsync(eventId, ct) ?? throw new NotFoundException("Event");
        return ev.Tables.Select(t => new TableDto(t.Id, t.Number, t.Capacity, t.MinSpend, t.Deposit, t.Status.ToString())).ToList();
    }
    static EventDto Map(Event e) => new(e.Id, e.Title, e.StartsAt, e.GuestListLimit, e.MinAge, e.IsClosed);
}

public class PromoterService(IPromoterRepository promoters, ICommissionRepository commissions, IGuestListRepository entries, IEventRepository events, IBookingRepository bookings, IUnitOfWork uow)
{
    public async Task<PromoterProfileDto> MeAsync(Guid guestId, CancellationToken ct)
    {
        var p = await promoters.GetByGuestIdAsync(guestId, ct) ?? throw new ForbiddenException("Not a promoter");
        return new PromoterProfileDto(p.Id, p.Name, p.Code, 0m);          
    }

    public async Task<List<PromoterGuestDto>> GuestsAsync(Guid guestId, Guid eventId, CancellationToken ct)
    {
        var p = await promoters.GetByGuestIdAsync(guestId, ct) ?? throw new ForbiddenException("Not a promoter");
        var ev = await events.GetAsync(eventId, ct) ?? throw new NotFoundException("Event");
        var windowStart = ev.StartsAt.AddHours(-72);
        var result = new List<PromoterGuestDto>();
        foreach (var e in await entries.ForEventAsync(eventId, ct))
        {
            var touch = await promoters.GetFirstTouchAsync(e.GuestId, windowStart, ev.StartsAt, ct);
            if (touch?.PromoterId == p.Id) result.Add(new PromoterGuestDto(e.Id, e.Name, e.Status.ToString(), null));
        }
        return result;
    }

    public async Task AttributeAsync(Guid guestId, string code, CancellationToken ct)
    {
        var p = await promoters.GetByCodeAsync(code, ct) ?? throw new NotFoundException("Promoter code");
        await promoters.AddTouchAsync(new PromoterTouch { GuestId = guestId, PromoterId = p.Id }, ct);
        await uow.SaveAsync(ct);
    }
}

public class ReportService(
    IEventRepository events, IBookingRepository bookings, IGuestListRepository entries,
    ICommissionRepository commissions, IPromoterRepository promoters)
{
    private sealed class PromoterAgg
    {
        public string Name = "";
        public int Reg;
        public int Ci;
        public decimal Comm;
    }

    public async Task<EventReportDto> ForEventAsync(Guid eventId, CancellationToken ct)
    {
        var ev = await events.GetAsync(eventId, ct) ?? throw new NotFoundException("Event");
        var bs = await bookings.GetByEventAsync(eventId, ct);
        var byStatus = bs.GroupBy(b => b.Status.ToString()).ToDictionary(g => g.Key, g => g.Count());
        var received = bs.Sum(b => b.DepositPaid);
        var credited = bs.Where(b => b.Status is BookingStatus.Confirmed or BookingStatus.Seated).Sum(b => b.DepositPaid);
        var kept = bs.Where(b => b.Status == BookingStatus.NoShow).Sum(b => b.DepositPaid);
        var refunded = bs.Where(b => b.Status == BookingStatus.Cancelled)
                         .Sum(b => b.RefundAmount(ev.StartsAt, DateTime.UtcNow));      
        var checkedIn = (await entries.ForEventAsync(eventId, ct)).Count(e => e.Status == GuestListStatus.CheckedIn);
        return new EventReportDto(bs.Count, byStatus, received, credited, kept, refunded, checkedIn);
    }

    public async Task<List<PromoterReportDto>> PromotersAsync(Guid eventId, CancellationToken ct)
    {
        var ev = await events.GetAsync(eventId, ct) ?? throw new NotFoundException("Event");
        var eventEntries = await entries.ForEventAsync(eventId, ct);
        var windowStart = ev.StartsAt.AddHours(-72);
        var rows = new Dictionary<Guid, PromoterAgg>();

        foreach (var e in eventEntries)
        {
            var touch = await promoters.GetFirstTouchAsync(e.GuestId, windowStart, ev.StartsAt, ct);
            if (touch is null) continue;

            if (!rows.TryGetValue(touch.PromoterId, out var row))
                rows[touch.PromoterId] = row = new PromoterAgg();

            var promoter = await promoters.GetAsync(touch.PromoterId, ct);
            if (promoter is not null) row.Name = promoter.Name;

            row.Reg++;
            if (e.Status == GuestListStatus.CheckedIn) row.Ci++;
        }

        foreach (var c in await commissions.ForEventAsync(eventId, ct))
        {
            if (!rows.TryGetValue(c.PromoterId, out var row))
                rows[c.PromoterId] = row = new PromoterAgg();
            row.Comm = c.TotalAmount;
        }

        return rows
            .Select(kv => new PromoterReportDto(
                kv.Key,
                kv.Value.Name,
                kv.Value.Reg,
                kv.Value.Ci,
                kv.Value.Reg == 0 ? 0d : Math.Round((double)kv.Value.Ci / kv.Value.Reg, 3),
                kv.Value.Comm))
            .ToList();
    }
}