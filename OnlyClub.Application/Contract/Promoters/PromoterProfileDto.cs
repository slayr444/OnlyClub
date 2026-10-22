namespace OnlyClub.Application.Contract.Promoters;

public record PromoterProfileDto(
    Guid Id,
    string Name,
    string Code,
    decimal TotalCommission);