using LibBot.Models.Configurations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace LibBot.ConfigureServicesExtensions;

public static class AddAuthDbHttpClientExtension
{
    public static IServiceCollection AddAuthDbHttpClient(this IServiceCollection services,
        IConfiguration configuration)
    {
        var authDbConfiguration =
            configuration.GetSection("AuthDbConfiguration").Get<AuthDbConfiguration>();
        services.AddHttpClient("AuthFirebase", httpClient =>
        {
            httpClient.BaseAddress = new Uri(authDbConfiguration.BaseAddress);
        });

        return services;
    }
}
