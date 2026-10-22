namespace OnlyClub.Application.Contract.GuestList;

public record GuestListEntryDto(
    Guid Id,
    Guid EventId,
    string Name,
    string Status,
    Guid? ParentEntryId);