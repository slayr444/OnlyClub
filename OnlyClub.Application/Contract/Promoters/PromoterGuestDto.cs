namespace OnlyClub.Application.Contract.Promoters;

public record PromoterGuestDto(
    Guid EntryId,
    string Name,
    string Status,
    decimal? CommissionLine);