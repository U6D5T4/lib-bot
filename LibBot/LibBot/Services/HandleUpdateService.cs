using System;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace LibBot.Services;

public class HandleUpdateService
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
}
