using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OnlyClub.Application.Ports;
using OnlyClub.Adapters.Security;    

namespace OnlyClub.Adapters;

public static class DependencyInjection
{
    public static IServiceCollection AddAdapters(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);

        services.AddSingleton<TmaInitDataValidator>();

        services.AddSingleton<IQrSigner, HmacQrSigner>();
        services.AddSingleton<IPaymentProvider, TestPaymentProvider>();
        services.AddSingleton<ITelegramBot, TelegramBot>();

        services.AddHttpClient(TelegramBot.HttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        return services;
    }
}