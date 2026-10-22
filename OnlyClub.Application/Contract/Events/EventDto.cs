namespace OnlyClub.Application.Contract.Events;

public record EventDto(
    Guid Id,
    string Title,
    DateTime StartsAt,
    int GuestListLimit,
    int MinAge,
    bool IsClosed);