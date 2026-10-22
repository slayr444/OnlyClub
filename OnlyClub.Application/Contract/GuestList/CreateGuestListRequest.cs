namespace OnlyClub.Application.Contract.GuestList;

public record CreateGuestListRequest(
    string Name,
    int Age,
    Guid? ParentEntryId);