namespace OnlyClub.Application.Contract.Events;

public record CreateEventTableRequest(
    int Number,
    int Capacity,
    decimal MinSpend,
    decimal Deposit);