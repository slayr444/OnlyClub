namespace OnlyClub.Application.Contract.Events;

public record CreateEventRequest(
    string Title,
    DateTime StartsAt,
    int GuestListLimit,
    int MinAge,
    List<CreateEventTableRequest> Tables);