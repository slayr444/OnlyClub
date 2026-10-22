using Microsoft.Extensions.DependencyInjection;
using OnlyClub.Application.Services;

namespace OnlyClub.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<BookingService>();
        services.AddScoped<GuestListService>();
        services.AddScoped<EventService>();
        services.AddScoped<PromoterService>();
        services.AddScoped<ReportService>();
        services.AddScoped<CommissionService>();
        services.AddScoped<ReminderScheduler>();

        return services;
    }
}