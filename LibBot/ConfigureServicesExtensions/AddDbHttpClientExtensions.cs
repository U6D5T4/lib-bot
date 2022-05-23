using LibBot.Models.Configurations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace LibBot.ConfigureServicesExtensions;

public static class AddDbHttpClientExtension
{
    public static IServiceCollection AddDbHttpClient(this IServiceCollection services,
        IConfiguration configuration)
    {
        var DbConfiguration =
            configuration.GetSection("DbConfiguration").Get<DbConfiguration>();
        services.AddHttpClient("Firebase", httpClient =>
        {
            httpClient.BaseAddress = new Uri(DbConfiguration.BasePath);
        });

        return services;
    }
}
