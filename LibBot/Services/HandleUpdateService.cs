using LibBot.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace LibBot.Services;

public class HandleUpdateService : IHandleUpdateService
{
    private readonly ITelegramBotClient _botClient;
    private readonly IMessageService _messageService;
    private readonly IUserService _userService;
    private readonly ISharePointService _sharePointService;
    public HandleUpdateService(ITelegramBotClient botClient, IMessageService messageService, IUserService userService, ISharePointService sharePointService)
    {
        _botClient = botClient;
        _messageService = messageService;
        _userService = userService;
        _sharePointService = sharePointService;
    }

    public async Task HandleAsync(Update update)
    {
        if (!await IsUserVerified(update.Message))
        {
            return;
        }

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

    private async Task<bool> IsUserVerified(Message message)
    {
        if (!_userService.IsUserExist(message.Chat.Id))
        {
            //TODO: write into db
            //Send message: "Send outlook username or outlook email"
            await _messageService.AskToEnterEmailOrUsername(_botClient, message);
            return false;
        }

        if (!_userService.WasAuthenticationCodeSendForUser(message.Chat.Id))
        {
            //TODO: check in SharePoint
         
            if (await _userService.IsLoginValid(message.Text))
            {
                var authToken = _userService.GenerateAuthCodeAndSaveItIntoDatabase();
                await _userService.SendEmailWithAuthToken(message.Text, message.Chat.Username, authToken);
                await _messageService.AskToEnterAuthTokenFromMail(_botClient, message);
            }
            else
            {
                await _messageService.AskToEnterEmailOrUsername(_botClient, message);
            }
            //Generate auth code, save it into db
            //Send message: send auth code from email
            return false;
        }

        if (!_userService.IsUserVerifyAccount(message.Chat.Id))
        {
            //TODO: check if code from user equals code in db
            if (!await _userService.VerifyAccount(message.Text, message.Chat.Id))
            {
               await _messageService.AskToEnterAuthTokenFromMail(_botClient, message);
            }
            //Mark user as verified
            //Send bot options and change keyboard
        }

        return true;
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

    private Task UnknownUpdateHandlerAsync(Update update)
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
