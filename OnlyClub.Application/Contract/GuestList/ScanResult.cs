namespace OnlyClub.Application.Contract.GuestList;

public record ScanResult(
    Guid EntryId,
    string Name,
    string Status);