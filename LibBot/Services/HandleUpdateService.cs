using LibBot.Models;
using LibBot.Models.SharePointRequests;
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
    private readonly IMessageService _messageService;
    private readonly IUserService _userService;
    private readonly ISharePointService _sharePointService;
    private readonly IChatService _chatService;

    public HandleUpdateService(ITelegramBotClient botClient, IMessageService messageService, IUserService userService, ISharePointService sharePointService, IChatService chatService)
    {
        _botClient = botClient;
        _messageService = messageService;
        _userService = userService;
        _sharePointService = sharePointService;
        _chatService = chatService;
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
                var login = _userService.ParseLogin(message.Text);
                var userData = await _sharePointService.GetUserDataFromSharePointAsync(login);
                if (userData != null)
                {
                    await _userService.UpdateUserDataAsync(chatId, userData);
                    await CreateAndSendAuthCodeAsync(chatId, message);
                    await _messageService.AskToEnterAuthCodeAsync(_botClient, message);
                }
                else
                {
                    return false;
                }
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
        switch (message.Text)
        {
            case "All Books":
                var chatInfoAllBooks = new ChatDbModel();
                chatInfoAllBooks.ChatId = message.Chat.Id;
                chatInfoAllBooks.ChatState = ChatState.AllBooks;
                chatInfoAllBooks.PageNumber = 0;
                chatInfoAllBooks.InlineMessageId = message.MessageId + 1;
                await _sharePointService.GetBooksFromSharePointAsync(chatInfoAllBooks.PageNumber);
                await _messageService.DisplayBookButtons(message, "These books are in our library.");
                await _chatService.SaveChatInfoAsync(chatInfoAllBooks);
                break;

            case "My Books":
                var chatInfoUserBooks = new ChatDbModel();
                chatInfoUserBooks.ChatId = message.Chat.Id;
                chatInfoUserBooks.ChatState = ChatState.UserBooks;
                chatInfoUserBooks.PageNumber = 0;
                chatInfoUserBooks.InlineMessageId = message.MessageId + 1;
                var user = await _userService.GetUserByChatIdAsync(message.Chat.Id);
                await _sharePointService.GetBooksFromSharePointAsync(chatInfoUserBooks.PageNumber, user.SharePointId);
                if (SharePointService.Books.Count != 0)
                    await _messageService.DisplayBookButtons(message, "These are your books.");
                else
                {
                    chatInfoUserBooks.PageNumber = 0;
                    await _messageService.DisplayBookButtons(message, "You don't read any books now");
                }
                await _chatService.SaveChatInfoAsync(chatInfoUserBooks);
                break;

            default:
                await _messageService.SayDefaultMessageAsync(_botClient, message);
                break;
        }
    }
    private async Task BotOnCallbackQueryReceived(CallbackQuery callbackQuery)
    {
        var data = await _chatService.GetChatInfoAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId);
        switch (callbackQuery.Data)
        {
            case "Next":
                if (SharePointService.Books.Count == SharePointService.AmountBooks)
                {
                    data.PageNumber = _sharePointService.SetNextPageNumberValue(data.PageNumber);                
                    await _chatService.UpdateChatInfoAsync(data);
                    await UpdateBooksLibrary(callbackQuery, data);
                    await UpdateInlineButtonsAsync(callbackQuery);
                }
            break;

            case "Previous":
                var res = (data.PageNumber - SharePointService.AmountBooks <= 0);
                if (!res)
                {
                    data.PageNumber =  _sharePointService.SetPreviousPageNumberValue(data.PageNumber);
                    await _chatService.UpdateChatInfoAsync(data);
                    await UpdateBooksLibrary(callbackQuery, data);
                    await UpdateInlineButtonsAsync(callbackQuery);
                }
                break;

            case "No":
                if (data.ChatState == ChatState.AllBooks)
                    await _messageService.EditMessageAfterYesAndNoButtons(_botClient, callbackQuery, "These books are in our library.");
                if (data.ChatState == ChatState.UserBooks)
                    await _messageService.EditMessageAfterYesAndNoButtons(_botClient, callbackQuery, "These are your books.");
                    await UpdateInlineButtonsAsync(callbackQuery);
                break;

            case "Yes":
                var user = await _userService.GetUserByChatIdAsync(callbackQuery.Message.Chat.Id);;

                if (data.ChatState == ChatState.AllBooks)
                {
                    ChangeBookStatusRequest borrowBook = new ChangeBookStatusRequest(user.SharePointId, user.SharePointId, DateTime.UtcNow, DateTime.UtcNow);
                    await _sharePointService.ChangeBookStatus(callbackQuery.Message.Chat.Id, data.BookId, borrowBook);
                    await UpdateBooksLibrary(callbackQuery, data);
                    await _messageService.EditMessageAfterYesAndNoButtons(_botClient, callbackQuery, "These books are in our library.");
                }
                
                if (data.ChatState == ChatState.UserBooks)
                {
                    ChangeBookStatusRequest returnBook = new ChangeBookStatusRequest(null, user.SharePointId, null, DateTime.UtcNow);
                    await _sharePointService.ChangeBookStatus(callbackQuery.Message.Chat.Id, data.BookId, returnBook);
                    await UpdateBooksLibrary(callbackQuery, data);
                    if (SharePointService.Books.Count != 0)
                        await _messageService.EditMessageAfterYesAndNoButtons(_botClient, callbackQuery, "These are your books.");
                    else
                    {
                        data.PageNumber = 0;
                        await _messageService.EditMessageAfterYesAndNoButtons(_botClient, callbackQuery, "You don't read any books now");
                    }
                }
                await UpdateInlineButtonsAsync(callbackQuery);
                break;

            default:
                if (data.ChatState == ChatState.AllBooks)
                {
                    await _messageService.CreateYesAndNoButtons(callbackQuery, "Are you sure you want to borrow this book?");
                    data.BookId = int.Parse(callbackQuery.Data);
                    await _chatService.UpdateChatInfoAsync(data);
                 
                }
                if (data.ChatState == ChatState.UserBooks)
                {
                    await _messageService.CreateYesAndNoButtons(callbackQuery, "Are are sure you want to return this book?");
                    data.BookId = int.Parse(callbackQuery.Data);
                   await _chatService.UpdateChatInfoAsync(data);
                }
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

    private async Task UpdateInlineButtonsAsync(CallbackQuery callbackQuery)
    {
        await _messageService.UpdateBookButtons(callbackQuery.Message);
    }

    private async Task UpdateBooksLibrary(CallbackQuery callbackQuery, ChatDbModel data)
    {
        if (data.ChatState == ChatState.AllBooks)
        {
            await _sharePointService.GetBooksFromSharePointAsync(data.PageNumber);
        }
        else
        {
            var userForNext = await _userService.GetUserByChatIdAsync(callbackQuery.Message.Chat.Id);
            await _sharePointService.GetBooksFromSharePointAsync(data.PageNumber, userForNext.SharePointId);
        }
    }
}
