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
    public HandleUpdateService(ITelegramBotClient botClient, IMessageService messageService, IUserService userService)
    {
        _botClient = botClient;
        _messageService = messageService;
        _userService = userService;
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
            await handler;
        }
        catch (Exception exception)
        {
            await HandleErrorAsync(exception);
        }
    }

    private async Task<bool> IsUserVerified(Message message)
    {
        var chatId = message.Chat.Id;
        if (await _userService.IsUserVerifyAccountAsync(chatId))
        {
            return true;
        }

        if (!await _userService.IsUserExistAsync(chatId))
        {
            await _userService.CreateUserAsync(chatId);
            await _messageService.SendTextMessageAndClearKeyboardAsync(_botClient, chatId, "Please, enter your outlook email or outlook login.");
            return false;
        }

        if (!await _userService.WasAuthenticationCodeSendForUserAsync(chatId))
        {
            if (await _userService.IsLoginValidAsync(message.Text))
            {
                try
                {
                    var authToken = await _userService.GenerateAuthCodeAndSaveItIntoDatabaseAsync(chatId);
                    await _userService.SendEmailWithAuthTokenAsync(message.Text, message.Chat.Username, authToken);
                    await _messageService.SendTextMessageAndClearKeyboardAsync(_botClient, chatId, "Please, enter authorization token from your email to confirm registration.");
                }
                catch 
                {
                    await _userService.RejectUserAuthCodeAsync(chatId);
                    await _messageService.SendTextMessageAndClearKeyboardAsync(_botClient, chatId,
                        "Something go wrong, try again enter outlook email or outlook login");
                }

            }
            else
            {
                await _messageService.SendTextMessageAndClearKeyboardAsync(_botClient, chatId,
                    "We can't find user with such credentials"
                    + Environment.NewLine
                    + "Please, enter your outlook email or outlook login.");
            }

            return false;
        }

        if (!await _userService.VerifyAccountAsync(message.Text, chatId))
        {
            await _messageService.SendTextMessageAndClearKeyboardAsync(_botClient, chatId,
                "You entered wrong code"
                + Environment.NewLine
                + "Please, enter authorization token from your email to confirm registration.");
            return false;
        }

        return true;
    }

    private async Task BotOnMessageReceived(Message message)
    {
        if (message.Type != MessageType.Text)
            return;


        var action = message.Text!.Split(' ')[0] switch
        {
            "/first" => _messageService.SayHelloFromAntonAsync(_botClient, message),
            "/second" => _messageService.SayHelloFromArtyomAsync(_botClient, message),
            _ => _messageService.SayDefaultMessageAsync(_botClient, message),
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
