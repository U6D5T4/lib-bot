using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace LibBot.Services;

public class ConfigureWebhook : IHostedService
{
    private readonly IServiceProvider _services;
    private readonly BotConfiguration _botConfiguration;

    public ConfigureWebhook(IServiceProvider serviceProvider,
                            IOptions<BotConfiguration> botConfiguration)
    {
        _services = serviceProvider;
        _botConfiguration = botConfiguration.Value;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();

        var webhookAddress = @$"{_botConfiguration.HostAddress}/bot/{_botConfiguration.BotToken}";
        await botClient.SetWebhookAsync(
            url: webhookAddress,
            allowedUpdates: Array.Empty<UpdateType>(),
            cancellationToken: cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();
        await botClient.DeleteWebhookAsync(cancellationToken: cancellationToken);
    }
}
