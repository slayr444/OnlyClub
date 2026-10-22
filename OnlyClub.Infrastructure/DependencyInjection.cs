using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OnlyClub.Application.Ports;
using OnlyClub.Infrastructure.Repositories;

namespace OnlyClub.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<OnlyClubDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IGuestListRepository, GuestListRepository>();
        services.AddScoped<IGuestRepository, GuestRepository>();
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IPromoterRepository, PromoterRepository>();
        services.AddScoped<ICommissionRepository, CommissionRepository>();
        services.AddScoped<IAuditRepository, AuditRepository>();
        services.AddScoped<IScheduledMessageRepository, ScheduledMessageRepository>();

        return services;
    }
}