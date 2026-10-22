namespace OnlyClub.Application.Contract.Promoters;

public record PromoterReportDto(
    Guid PromoterId,
    string Name,
    int Registered,
    int CheckedIn,
    double Conversion,
    decimal Commission);