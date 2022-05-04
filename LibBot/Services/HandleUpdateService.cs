﻿using LibBot.Models;
using LibBot.Models.SharePointRequests;
using LibBot.Models.SharePointResponses;
using LibBot.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web;
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
                var chatInfoAllBooks = new ChatDbModel(message.Chat.Id, message.MessageId + 1, ChatState.AllBooks);
                var allBooks = await _sharePointService.GetBooksFromSharePointAsync(chatInfoAllBooks.PageNumber);
                await _messageService.DisplayBookButtons(message, "These books are in our library.", allBooks);
                await _chatService.SaveChatInfoAsync(chatInfoAllBooks);
                break;

            case "My Books":
                var chatInfoUserBooks = new ChatDbModel(message.Chat.Id, message.MessageId + 1, ChatState.UserBooks);
                var user = await _userService.GetUserByChatIdAsync(message.Chat.Id);
                var myBooks = await _sharePointService.GetBooksFromSharePointAsync(chatInfoUserBooks.PageNumber, user.SharePointId);
                if (myBooks.Count != 0)
                    await _messageService.DisplayBookButtons(message, "These are your books.", myBooks);
                else
                {
                    chatInfoUserBooks.PageNumber = 0;
                    await _messageService.DisplayBookButtons(message, "You don't read any books now", myBooks);
                }
                await _chatService.SaveChatInfoAsync(chatInfoUserBooks);
                break;

            case "Search Books":
                var chatInfoSearchBooks = new ChatDbModel(message.Chat.Id, message.MessageId, ChatState.SearchBooks);
                await _chatService.SaveChatInfoAsync(chatInfoSearchBooks);
                await _messageService.AksToEnterSearchQueryAsync(_botClient,message);
                break;
            default:
                var chatInfo = await _chatService.GetChatInfoAsync(message.Chat.Id, message.MessageId  - 2);
                if (chatInfo is not null && chatInfo.ChatState == ChatState.SearchBooks)
                {

                    chatInfo.SearchQuery =  HttpUtility.UrlEncode(message.Text.Trim());

                    await _chatService.SaveChatInfoAsync(chatInfo);
                    var searchBooks = await _sharePointService.GetBooksFromSharePointAsync(chatInfo.PageNumber, chatInfo.SearchQuery);
                    await _messageService.DisplayBookButtons(message, "This is the result of your search query.", searchBooks);
                }
                else
                    await _messageService.SayDefaultMessageAsync(_botClient, message);
                break;
        }
    }
    private async Task BotOnCallbackQueryReceived(CallbackQuery callbackQuery)
    {
        var data = await _chatService.GetChatInfoAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId);

        data = data is null ? await _chatService.GetChatInfoAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId - 3): data;

        switch (callbackQuery.Data)
        {
            case "Next":
                if (data.ChatState == ChatState.AllBooks)
                {
                    var books = await _sharePointService.GetBooksFromSharePointAsync(data.PageNumber + 1);
                    if (books.Count != 0)
                    {
                        data.PageNumber++;
                        await _chatService.UpdateChatInfoAsync(data);
                        await UpdateInlineButtonsAsync(callbackQuery, books);
                    }
                }
                if (data.ChatState == ChatState.SearchBooks)
                {
                    var books = await _sharePointService.GetBooksFromSharePointAsync(data.PageNumber + 1, data.SearchQuery);
                    if (books.Count != 0)
                    {
                        data.PageNumber++;
                        await _chatService.UpdateChatInfoAsync(data);
                        await UpdateInlineButtonsAsync(callbackQuery, books);
                    }
                }
                    break;

            case "Previous":
                if (data.ChatState == ChatState.AllBooks)
                {
                    if (data.PageNumber - 1 >= 0)
                    {
                        var previousBooks = await _sharePointService.GetBooksFromSharePointAsync(--data.PageNumber);
                        await _chatService.UpdateChatInfoAsync(data);
                        await UpdateInlineButtonsAsync(callbackQuery, previousBooks);
                    }
                }
                if (data.ChatState == ChatState.SearchBooks)
                {
                    if (data.PageNumber - 1 >= 0)
                    {
                        var previousBooks = await _sharePointService.GetBooksFromSharePointAsync(--data.PageNumber, data.SearchQuery);
                        await _chatService.UpdateChatInfoAsync(data);
                        await UpdateInlineButtonsAsync(callbackQuery, previousBooks);
                    }
                }
                break;

            case "No":
                if (data.ChatState == ChatState.AllBooks)
                {
                    var booksAfterNo = await _sharePointService.GetBooksFromSharePointAsync(data.PageNumber);
                    await _messageService.EditMessageAfterYesAndNoButtons(_botClient, callbackQuery, "These books are in our library.");
                    await UpdateInlineButtonsAsync(callbackQuery, booksAfterNo);
                }
                if (data.ChatState == ChatState.UserBooks)
                {
                    var userForNo = await _userService.GetUserByChatIdAsync(data.ChatId);
                    var booksAfterNo = await _sharePointService.GetBooksFromSharePointAsync(data.PageNumber, userForNo.SharePointId);
                    await _messageService.EditMessageAfterYesAndNoButtons(_botClient, callbackQuery, "These are your books.");
                    await UpdateInlineButtonsAsync(callbackQuery, booksAfterNo);
                }
                if (data.ChatState == ChatState.SearchBooks)
                { 
                    var booksAfterNo = await _sharePointService.GetBooksFromSharePointAsync(data.PageNumber,data.SearchQuery);
                    await _messageService.EditMessageAfterYesAndNoButtons(_botClient, callbackQuery, "This is the result of your search query.");
                    await UpdateInlineButtonsAsync(callbackQuery, booksAfterNo);
                }
                break;

            case "Yes":
                var user = await _userService.GetUserByChatIdAsync(callbackQuery.Message.Chat.Id);
                if (data.ChatState == ChatState.AllBooks)
                {
                    ChangeBookStatusRequest borrowBook = new ChangeBookStatusRequest(user.SharePointId, user.SharePointId, DateTime.UtcNow, DateTime.UtcNow);
                    await _sharePointService.ChangeBookStatus(callbackQuery.Message.Chat.Id, data.BookId, borrowBook);
                    var allBooksAfterYes = await UpdateBooksLibrary(callbackQuery, data);
                    await _messageService.EditMessageAfterYesAndNoButtons(_botClient, callbackQuery, "These books are in our library.");
                    await UpdateInlineButtonsAsync(callbackQuery, allBooksAfterYes);
                }

                if (data.ChatState == ChatState.UserBooks)
                {
                    ChangeBookStatusRequest returnBook = new ChangeBookStatusRequest(null, user.SharePointId, null, DateTime.UtcNow);
                    await _sharePointService.ChangeBookStatus(callbackQuery.Message.Chat.Id, data.BookId, returnBook);
                    var userBooksAfterYes = await UpdateBooksLibrary(callbackQuery, data);
                    if (userBooksAfterYes.Count != 0)
                        await _messageService.EditMessageAfterYesAndNoButtons(_botClient, callbackQuery, "These are your books.");
                    else
                    {
                        data.PageNumber = 0;
                        await _messageService.EditMessageAfterYesAndNoButtons(_botClient, callbackQuery, "You don't read any books now");
                    }
                    await UpdateInlineButtonsAsync(callbackQuery, userBooksAfterYes);
                }

                if (data.ChatState == ChatState.SearchBooks)
                {
                    ChangeBookStatusRequest borrowBook = new ChangeBookStatusRequest(user.SharePointId, user.SharePointId, DateTime.UtcNow, DateTime.UtcNow);
                    await _sharePointService.ChangeBookStatus(callbackQuery.Message.Chat.Id, data.BookId, borrowBook);
                    var booksAfterYes = await _sharePointService.GetBooksFromSharePointAsync(data.PageNumber, data.SearchQuery);
                    await _messageService.EditMessageAfterYesAndNoButtons(_botClient, callbackQuery, "This is the result of your search query.");
                    await UpdateInlineButtonsAsync(callbackQuery, booksAfterYes);
                }

                break;

            case "Borrowed":
                    await _messageService.SayThisBookIsAlreadyBorrowAsync(_botClient, callbackQuery.Message);

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
                if (data.ChatState == ChatState.SearchBooks)
                {
                    await _messageService.CreateYesAndNoButtons(callbackQuery, "Are you sure you want to borrow this book?");
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

    private async Task UpdateInlineButtonsAsync(CallbackQuery callbackQuery, List<BookDataResponse> books)
    {
        await _messageService.UpdateBookButtons(callbackQuery.Message, books);
    }

    private async Task<List<BookDataResponse>> UpdateBooksLibrary(CallbackQuery callbackQuery, ChatDbModel data)
    {
        if (data.ChatState == ChatState.AllBooks)
        {
            return await _sharePointService.GetBooksFromSharePointAsync(data.PageNumber);
        }
        else
        {
            var userForNext = await _userService.GetUserByChatIdAsync(callbackQuery.Message.Chat.Id);
            return await _sharePointService.GetBooksFromSharePointAsync(data.PageNumber, userForNext.SharePointId);
        }
    }
}
