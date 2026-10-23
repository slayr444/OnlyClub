using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using OnlyClub.Application.Contract.Admin;
using OnlyClub.Application.Contract.Bookings;
using OnlyClub.Application.Contract.Events;
using OnlyClub.Application.Contract.GuestList;
using OnlyClub.Application.Contract.Payments;
using OnlyClub.Application.Ports;
using OnlyClub.Application.Services;
using OnlyClub.Core.Enums;
using OnlyClub.Infrastructure;
using System.Security.Claims;

namespace OnlyClub.API.Endpoints;

public static class ApiEndpoints
{
    public static void MapOnlyClubEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api");

        var guestApi = api.MapGroup("").RequireAuthorization();

        guestApi.MapGet("/events", async ([FromServices] EventService svc, CancellationToken ct) =>
        {
            var list = await svc.ListAsync(ct);
            return Results.Ok(list);
        });

        guestApi.MapGet("/events/{id:guid}/tables", async (Guid id, [FromServices] EventService svc, CancellationToken ct) =>
        {
            var tables = await svc.TablesAsync(id, ct);
            return Results.Ok(tables);
        });

        guestApi.MapGet("/me", async (ClaimsPrincipal user, [FromServices] IGuestRepository guests, CancellationToken ct) =>
        {
            var guestId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var g = await guests.GetAsync(guestId, ct) ?? throw new Exception("Гость не найден");
            return Results.Ok(new { name = g.Name, telegramId = g.TelegramId, role = g.Role.ToString() });
        });

        guestApi.MapGet("/bookings/my", async (ClaimsPrincipal user, [FromServices] IBookingRepository bookings, CancellationToken ct) =>
        {
            var guestId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var list = await bookings.GetByGuestAsync(guestId, ct);

            return Results.Ok(list.Select(b => new {
                id = b.Id,
                tableNumber = b.EventTable.Number.ToString(),
                partySize = b.PartySize,
                status = b.Status.ToString(),
                depositPaid = b.DepositPaid,
                remainingMinSpend = Math.Max(0m, b.EventTable.MinSpend - b.DepositPaid),
                qrToken = b.QrToken,
                heldUntilAt = b.HeldUntilAt,
                deposit = b.EventTable.Deposit,
                title = b.EventTable.Event.Title
            }));
        });

        guestApi.MapPost("/bookings", async (ClaimsPrincipal user, [FromBody] CreateBookingRequest req, [FromServices] BookingService svc, CancellationToken ct) =>
        {
            var guestId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var res = await svc.CreateAsync(guestId, req, ct);
            return Results.Ok(res);
        });

        guestApi.MapPost("/bookings/{id:guid}/pay", async (Guid id, ClaimsPrincipal user, [FromServices] BookingService svc, CancellationToken ct) =>
        {
            var guestId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var res = await svc.InitiatePaymentAsync(id, guestId, ct);

            if (res.Success && res.ProviderRef != null)
            {
                await svc.HandlePaymentWebhookAsync(new PaymentWebhookRequest(res.ProviderRef, "succeeded"), ct);
            }

            return Results.Ok(res);
        });

        guestApi.MapPost("/bookings/{id:guid}/cancel", async (Guid id, ClaimsPrincipal user, [FromServices] BookingService svc, CancellationToken ct) =>
        {
            var guestId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var res = await svc.CancelAsync(id, guestId, isManager: false, ct);
            return Results.Ok(res);
        });

        guestApi.MapGet("/guestlist/my", async (ClaimsPrincipal user, [FromServices] OnlyClubDbContext db, CancellationToken ct) =>
        {
            var guestId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var list = await db.GuestListEntries
                .Include(e => e.Event)
                .Where(e => e.GuestId == guestId)
                .Select(e => new { id = e.Id, name = e.Event.Title, status = e.Status.ToString() })
                .ToListAsync(ct);
            return Results.Ok(list);
        });

        guestApi.MapPost("/events/{id:guid}/guestlist", async (Guid id, ClaimsPrincipal user, [FromBody] CreateGuestListRequest req, [FromServices] GuestListService svc, CancellationToken ct) =>
        {
            var guestId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var res = await svc.RegisterAsync(guestId, id, req, ct);
            return Results.Ok(res);
        });

        guestApi.MapGet("/guestlist/{id:guid}/qr", async (Guid id, [FromServices] GuestListService svc, CancellationToken ct) =>
        {
            var token = await svc.GetQrAsync(id, ct);
            return Results.Ok(new { qr = token });
        });

        guestApi.MapPost("/guestlist/{id:guid}/cancel", async (Guid id, ClaimsPrincipal user, [FromServices] GuestListService svc, CancellationToken ct) =>
        {
            var guestId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            await svc.CancelAsync(id, guestId, ct);
            return Results.Ok(new { message = "Заявка отменена" });
        });

        api.MapPost("/webhooks/payments", async ([FromBody] PaymentWebhookRequest req, [FromServices] BookingService svc, CancellationToken ct) =>
        {
            await svc.HandlePaymentWebhookAsync(req, ct);
            return Results.Ok();
        }).AllowAnonymous();

        var promoterApi = api.MapGroup("/promoter")
            .RequireAuthorization(policy => policy.RequireRole(Role.Promoter.ToString()));

        promoterApi.MapGet("/me", async (ClaimsPrincipal user, [FromServices] PromoterService svc, CancellationToken ct) =>
        {
            var guestId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var res = await svc.MeAsync(guestId, ct);
            return Results.Ok(res);
        });

        promoterApi.MapGet("/events/{id:guid}/guests", async (Guid id, ClaimsPrincipal user, [FromServices] PromoterService svc, CancellationToken ct) =>
        {
            var guestId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var res = await svc.GuestsAsync(guestId, id, ct);
            return Results.Ok(res);
        });

        var guardApi = api.MapGroup("")
            .RequireAuthorization(policy => policy.RequireRole(Role.Guard.ToString(), Role.Manager.ToString()));

        guardApi.MapPost("/checkin/scan", async (ClaimsPrincipal user, [FromBody] ScanRequest req, [FromServices] GuestListService svc, CancellationToken ct) =>
        {
            var guardId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var guardTgId = long.Parse(user.FindFirst("telegram_id")?.Value ?? "0");
            var res = await svc.ScanAsync(req, guardId, guardTgId, ct);
            return Results.Ok(res);
        });

        guardApi.MapPost("/bookings/{id:guid}/seat", async (Guid id, ClaimsPrincipal user, [FromServices] BookingService svc, CancellationToken ct) =>
        {
            var guardId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            await svc.SeatAsync(id, guardId, ct);
            return Results.Ok(new { status = "Seated" });
        });

        var managerApi = api.MapGroup("")
            .RequireAuthorization(policy => policy.RequireRole(Role.Manager.ToString()));

        managerApi.MapPost("/events", async ([FromBody] CreateEventRequest req, [FromServices] EventService svc, CancellationToken ct) =>
        {
            var res = await svc.CreateAsync(req, ct);
            return Results.Ok(res);
        });

        managerApi.MapPost("/events/{id:guid}/close", async (Guid id, ClaimsPrincipal user, [FromServices] CommissionService svc, CancellationToken ct) =>
        {
            var managerId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            await svc.CloseAsync(id, managerId, ct);
            return Results.Ok(new { message = "Event closed, commissions calculated" });
        });

        managerApi.MapPost("/events/{id:guid}/reopen", async (Guid id, [FromBody] ReopenRequest req, ClaimsPrincipal user, [FromServices] CommissionService svc, CancellationToken ct) =>
        {
            var managerId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            await svc.ReopenAsync(id, req.Reason, managerId, ct);
            return Results.Ok(new { message = "Event reopened, commissions recalculated" });
        });

        managerApi.MapPost("/commissions/{id:guid}/approve", async (Guid id, ClaimsPrincipal user, [FromServices] CommissionService svc, CancellationToken ct) =>
        {
            var managerId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            await svc.ApproveAsync(id, managerId, ct);
            return Results.Ok(new { status = "Approved" });
        });

        managerApi.MapPost("/commissions/{id:guid}/pay", async (Guid id, ClaimsPrincipal user, [FromServices] CommissionService svc, CancellationToken ct) =>
        {
            var managerId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            await svc.PayAsync(id, managerId, ct);
            return Results.Ok(new { status = "Paid" });
        });

        managerApi.MapGet("/reports/events/{id:guid}", async (Guid id, [FromServices] ReportService svc, CancellationToken ct) =>
        {
            var res = await svc.ForEventAsync(id, ct);
            return Results.Ok(res);
        });
    }
}