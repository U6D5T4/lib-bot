using System;
using System.Net;
using System.Net.Http;
using LibBot.Models.Configurations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LibBot.ConfigureServicesExtensions;

public static class AddSharePointHttpClientExtension
{
    public static IServiceCollection AddSharePointHttpClient(this IServiceCollection services,
        IConfiguration configuration)
    {
        var botCredentials = configuration.GetSection("BotCredentials").Get<BotCredentialsConfiguration>();
        var sharePointConfiguration =
            configuration.GetSection("SharePointConfiguration").Get<SharePointConfiguration>();
        services.AddHttpClient("SharePoint", httpClient =>
        {
            httpClient.BaseAddress = new Uri(sharePointConfiguration.BaseAddress);
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json;Odata=verbose");
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            Credentials = new NetworkCredential(botCredentials.Login, botCredentials.Password)
        });

        return services;
    }
}