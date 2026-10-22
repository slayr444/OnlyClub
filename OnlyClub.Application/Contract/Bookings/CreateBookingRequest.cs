namespace OnlyClub.Application.Contract.Bookings;

public record CreateBookingRequest(
    Guid EventTableId,
    int PartySize);