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
    private readonly IBookService _bookService;
    public HandleUpdateService(ITelegramBotClient botClient, IMessageService messageService, IUserService userService, ISharePointService sharePointService, IBookService bookService)
    {
        _botClient = botClient;
        _messageService = messageService;
        _userService = userService;
        _sharePointService = sharePointService;
        _bookService = bookService;
    }

    public async Task HandleAsync(Update update)
    {
        try
        {
            switch (update.Type)
            {
                case UpdateType.Message:
                    if (!await HandleAuthenticationAsync(update.Message))
                    {
                        return;
                    }
                    await BotOnMessageReceived(update.Message!);
                    break;
                case UpdateType.CallbackQuery:
                    await BotOnCallbackQueryReceived(update.CallbackQuery!);
                    break;
                default:
                    break;
            };
        }
        catch (Exception exception)
        {
            await HandleErrorAsync(exception);
        }
    }
    
    private async Task<bool> HandleAuthenticationAsync(Message message)
    {
        var chatId = message.Chat.Id;
        if (await _userService.IsUserVerifyAccountAsync(chatId))
        {
            return true;
        }

        if (!await _userService.IsUserExistAsync(chatId))
        {
            await _userService.CreateUserAsync(chatId);
            await _messageService.AskToEnterOutlookLoginAsync(_botClient, message);
            return false;
        }

        if (!await _userService.WasAuthenticationCodeSendForUserAsync(chatId))
        {
            if (await _userService.IsLoginValidAsync(message.Text))
            {
                await _userService.UpdateUserEmailAsync(chatId, message.Text);
                await CreateAndSendAuthCodeAsync(chatId, message);
                await _messageService.AskToEnterAuthCodeAsync(_botClient, message);
            }
            else
            {
                await _messageService.SendTextMessageAndClearKeyboardAsync(_botClient, chatId, "We can't find user with such credentials");
                await _messageService.AskToEnterOutlookLoginAsync(_botClient, message);
            }

            return false;
        }

        if (await _userService.IsCodeLifetimeExpiredAsync(chatId))
        {
            await CreateAndSendAuthCodeAsync(chatId, message);
            await _messageService.SendTextMessageAndClearKeyboardAsync(_botClient, chatId, "Code lifetime was expired.");
            await _messageService.AskToEnterAuthCodeAsync(_botClient, message);
            return false;
        }

        if (!await _userService.VerifyAccountAsync(message.Text, chatId))
        {
            await _messageService.SendTextMessageAndClearKeyboardAsync(_botClient, chatId, "You entered wrong code ");
            await _messageService.AskToEnterAuthCodeAsync(_botClient, message);
            return false;
        }

        return true;
    }

    private async Task CreateAndSendAuthCodeAsync(long chatId, Message message)
    {
        try
        {
            var authCode = await _userService.GenerateAuthCodeAndSaveItIntoDatabaseAsync(chatId);
            await _userService.SendEmailWithAuthCodeAsync(chatId, message.Chat.Username, authCode);
        }
        catch
        {
            await _userService.RejectUserAuthCodeAsync(chatId);
            await _messageService.SendTextMessageAndClearKeyboardAsync(_botClient, chatId,
                "Something go wrong, try again enter outlook email or outlook login");
        }
    }
    
    private async Task BotOnMessageReceived(Message message)
    {
        if (message.Type != MessageType.Text)
            return;

       switch (message.Text) {
            case "/first":
               await _messageService.SayHelloFromAntonAsync(_botClient, message);
                break;

            case "/second":
                await _messageService.SayHelloFromArtyomAsync(_botClient, message);
                break;

            case "All Books":
                _sharePointService.SetDefaultPageNumberValue();
                var books = await _sharePointService.GetBooksFromSharePointAsync();
                await _bookService.DisplayBookButtons(_botClient, message, books);
                break;

            default:
                await _messageService.SayDefaultMessageAsync(_botClient, message);
                break;

        }

        await SetCommand();
    }
    private async Task BotOnCallbackQueryReceived(CallbackQuery callbackQuery)
    {
        switch (callbackQuery.Data)
        {
            case "Next":
                _sharePointService.SetNextPageNumberValue();
                var booksNext = await _sharePointService.GetBooksFromSharePointAsync();
                await _bookService.UpdateBookButtons(_botClient, callbackQuery.Message, booksNext);
                break;

            case "Previous":
                _sharePointService.SetPreviousPageNumberValue();
                var booksPrevious = await _sharePointService.GetBooksFromSharePointAsync();
                await _bookService.UpdateBookButtons(_botClient, callbackQuery.Message, booksPrevious);
                break;

            default:
                break;
         
        }
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
