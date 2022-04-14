using LibBot.Services.Interfaces;
using System;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace LibBot.Services;

public class HandleUpdateService : IHandleUpdateService
{
    private readonly ITelegramBotClient _botClient;

    public HandleUpdateService(ITelegramBotClient botClient)
    {
        _botClient = botClient;
    }

    public async Task EchoAsync(Update update)
    {
        await _botClient.SendTextMessageAsync(update.Message.Chat.Id, update.Message.Text);
    }

    public async Task SayHelloFromAnton(Update update)
    {
        await _botClient.SendTextMessageAsync(update.Message.Chat.Id, "Hello, this is Anton's function!");
    }

    public async Task SayHelloFromArtyom(Update update)
    {
        await _botClient.SendTextMessageAsync(update.Message.Chat.Id, "Hello, this is Artyom's function!");
    }
}
