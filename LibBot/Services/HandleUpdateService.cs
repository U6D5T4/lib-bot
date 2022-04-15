using LibBot.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace LibBot.Services;

public class HandleUpdateService : IHandleUpdateService
{
    private readonly ITelegramBotClient _botClient;
    
    private readonly IMessageService _messageService;
    public HandleUpdateService(ITelegramBotClient botClient, IMessageService messageService)
    {
        _botClient = botClient;
        _messageService = messageService;
    }

    public async Task EchoAsync(Update update)
    {

        var handler = update.Type switch
        {
            UpdateType.Message => BotOnMessageReceived(update.Message!),
            _ => UnknownUpdateHandlerAsync(update)
        };

        try
        {
            await (Task)handler;
        }
        catch (Exception exception)
        {
            await HandleErrorAsync(exception);
        }
    }

    private async Task BotOnMessageReceived(Message message)
    {
        if (message.Type != MessageType.Text)
            return;

        var action = message.Text!.Split(' ')[0] switch
        {
            "/first" => _messageService.SayHelloFromAnton(_botClient, message),
            "/second" => _messageService.SayHelloFromArtyom(_botClient, message),
            _ => _messageService.SayDefaultMessage(_botClient, message),
        };

        Message sentMessage = await action;

        await SetCommand();
    }

    private object UnknownUpdateHandlerAsync(Update update)
    {
        return Task.CompletedTask;
    }

    public Task HandleErrorAsync(Exception exception)
    {
        var ErrorMessage = exception switch
        {
            ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        return Task.CompletedTask;
    }
    private Task SetCommand()
    {
        return _botClient.SetMyCommandsAsync(new List<BotCommand>() {
            new BotCommand()
            {
                Command = "first",
                Description = "First command description"
            },
            new BotCommand()
            {
                Command = "second",
                Description = "Second command description"
            }
        });
    }
}
