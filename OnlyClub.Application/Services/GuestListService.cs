using OnlyClub.Application.Contract.GuestList;
using OnlyClub.Application.Ports;
using OnlyClub.Core;
using OnlyClub.Core.Common;
using OnlyClub.Core.Entities;
using OnlyClub.Core.Enums;

namespace OnlyClub.Application.Services;

public class GuestListService(
    IGuestListRepository entries, IEventRepository events, IPromoterRepository promoters,
    IQrSigner qr, IAuditRepository audit, IUnitOfWork uow)
{
    public async Task<GuestListEntryDto> RegisterAsync(Guid guestId, Guid eventId, CreateGuestListRequest req, CancellationToken ct)
    {
        var ev = await events.GetAsync(eventId, ct) ?? throw new NotFoundException("Event");
        if (ev.IsClosed) throw new DomainException("Event closed");
        if (req.Age < ev.MinAge) throw new DomainException($"Minimum age {ev.MinAge}");
        if (await entries.GetActiveForGuestAsync(eventId, guestId, ct) is not null) throw new DomainException("Already registered");

        if (req.ParentEntryId.HasValue)
        {
            var parent = await entries.GetAsync(req.ParentEntryId.Value, ct) ?? throw new NotFoundException("Parent");
            if (parent.EventId != eventId) throw new DomainException("Parent from another event");
            var companions = await entries.CountCompanionsAsync(parent.Id, ct);
            if (companions >= 3) throw new DomainException("Max 3 companions");
        }
        else
        {
            var count = await entries.CountAsync(eventId, ct);
            if (count + 1 > ev.GuestListLimit) throw new DomainException("Guest list full");
        }

        var entry = new GuestListEntry { EventId = eventId, GuestId = guestId, Name = req.Name, ParentEntryId = req.ParentEntryId };
        entry.Confirm();          
        await entries.AddAsync(entry, ct);
        await uow.SaveAsync(ct);
        return new GuestListEntryDto(entry.Id, eventId, entry.Name, entry.Status.ToString(), entry.ParentEntryId);
    }

    public async Task<ScanResult> ScanAsync(ScanRequest req, Guid guardGuestId, long guardTgId, CancellationToken ct)
    {
        if (!qr.TryParse(req.Qr, out var entryId, out var eventId)) throw new DomainException("Invalid QR");
        var entry = await entries.GetAsync(entryId, ct) ?? throw new NotFoundException("Entry");
        if (entry.EventId != eventId) throw new DomainException("Event mismatch");
        if (entry.Event.IsClosed) throw new DomainException("Event closed");
        if (entry.Status == GuestListStatus.CheckedIn) throw new DomainException("Already checked in");
        if (!req.AgeVerified) throw new DomainException("Age not verified");
        entry.AgeVerifiedByGuard = true;
        entry.CheckIn(guardTgId);
        await audit.AddAsync(new AuditLog { ActorId = guardGuestId, Action = AuditAction.CheckIn, EntityType = "GuestListEntry", EntityId = entry.Id }, ct);
        await uow.SaveAsync(ct);
        return new ScanResult(entry.Id, entry.Name, entry.Status.ToString());
    }

    public async Task<string> GetQrAsync(Guid entryId, CancellationToken ct)
    {
        var e = await entries.GetAsync(entryId, ct) ?? throw new NotFoundException("Entry");
        return qr.Sign(e.Id, e.EventId);
    }
}