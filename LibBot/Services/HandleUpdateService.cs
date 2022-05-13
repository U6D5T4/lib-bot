using LibBot.Models;
using LibBot.Models.SharePointRequests;
using LibBot.Models.SharePointResponses;
using LibBot.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace LibBot.Services;

public class HandleUpdateService : IHandleUpdateService
{
 
    private readonly IMessageService _messageService;
    private readonly IUserService _userService;
    private readonly ISharePointService _sharePointService;
    private readonly IChatService _chatService;

    public HandleUpdateService(IMessageService messageService, IUserService userService, ISharePointService sharePointService, IChatService chatService)
    {
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
            await _messageService.AskToEnterOutlookLoginAsync(message);
            return false;
        }

        if (!await _userService.WasAuthenticationCodeSendForUserAsync(chatId))
        {
            return await GenerateAndSendAuthCodeAsync(message);
        }

        if (await _userService.IsCodeLifetimeExpiredAsync(chatId))
        {
            await CreateAndSendAuthCodeAsync(chatId, message);
            await _messageService.SendTextMessageAndClearKeyboardAsync(chatId, "Code lifetime was expired.");
            await _messageService.AskToEnterAuthCodeAsync(message);
            return false;
        }

        if (!await _userService.VerifyAccountAsync(message.Text, chatId))
        {
            await _messageService.SendTextMessageAndClearKeyboardAsync(chatId, "You entered wrong code ");
            await _messageService.AskToEnterAuthCodeAsync(message);
            return false;
        }

        return true;
    }

    private async Task<bool> GenerateAndSendAuthCodeAsync(Message message)
    {
        if (await _userService.IsLoginValidAsync(message.Text))
        {
            var login = _userService.ParseLogin(message.Text);
            var userData = await _sharePointService.GetUserDataFromSharePointAsync(login);
            if (userData != null)
            {
                await _userService.UpdateUserDataAsync(message.Chat.Id, userData);
                await CreateAndSendAuthCodeAsync(message.Chat.Id, message);
                await _messageService.AskToEnterAuthCodeAsync(message);
            }
        }
        else
        {
            await _messageService.SendTextMessageAndClearKeyboardAsync(message.Chat.Id, "We can't find user with such credentials");
            await _messageService.AskToEnterOutlookLoginAsync(message);
        }

        return false;
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
            await _messageService.SendTextMessageAndClearKeyboardAsync(chatId,
                "Something go wrong, try again enter outlook email or outlook login");
            throw;
        }
    }

    private async Task BotOnMessageReceived(Message message)
    {
        if (message.Type != MessageType.Text)
            return;
        switch (message.Text.ToLower())
        {
            case "library":
                await _messageService.SendLibraryMenuMessageAsync(message.Chat.Id);
                var user = await _userService.GetUserByChatIdAsync(message.Chat.Id);
                user.MenuState = MenuState.Library;
                await _userService.UpdateUserAsync(user);
                break;

            case "filter by path":
                await _messageService.SendFilterMenuMessageWithKeyboardAsync(message.Chat.Id);
                await HandleFilterByPathOptionAsync(message, message.MessageId + 2);
                break;

            case "clear filters":
                await HandleFilterByPathOptionAsync(message, message.MessageId + 1);
                break;

            case "show filtered":
                await HandleShowFilteredOptionAsync(message);
                break;

            case "show all books":
                var chatInfoAllBooks = new ChatDbModel(message.Chat.Id, message.MessageId + 1, ChatState.AllBooks);
                var allBooks = await GetBookDataResponses(chatInfoAllBooks.PageNumber, chatInfoAllBooks);
                await _messageService.DisplayBookButtons(chatInfoAllBooks.ChatId,
                    "These books are in our library", allBooks, chatInfoAllBooks.ChatState);
                await _chatService.SaveChatInfoAsync(chatInfoAllBooks);
                break;

            case "my books":
                var chatInfoUserBooks = new ChatDbModel(message.Chat.Id, message.MessageId + 1, ChatState.UserBooks);
                user = await _userService.GetUserByChatIdAsync(message.Chat.Id);
                var myBooks = await _sharePointService.GetBooksAsync(chatInfoUserBooks.PageNumber, user.SharePointId);
                if (myBooks.Count != 0)
                {
                    await _messageService.CreateUserBookButtonsAsync(chatInfoUserBooks.ChatId, myBooks);
                }
                else
                {
                    chatInfoUserBooks.PageNumber = 0;
                    await _messageService.DisplayBookButtons(chatInfoUserBooks.ChatId, "You don't read any books now", myBooks, chatInfoUserBooks.ChatState);
                }
                await _chatService.SaveChatInfoAsync(chatInfoUserBooks);
                break;

            case "search books":
                var chatInfoSearchBooks = new ChatDbModel(message.Chat.Id, message.MessageId, ChatState.SearchBooks);
                await _chatService.SaveChatInfoAsync(chatInfoSearchBooks);
                await _messageService.AksToEnterSearchQueryAsync(message);
                break;

            case "help":
                await _messageService.SendHelpMenuAsync(message.Chat.Id);
                user = await _userService.GetUserByChatIdAsync(message.Chat.Id);
                user.MenuState = MenuState.Help;
                await _userService.UpdateUserAsync(user);
                break;

            case "about":
                
                await _messageService.SendTextMessageAsync(message.Chat.Id, $"@U6LibBot_bot, v{GetBotVersion()}");
                break;

            case "feedback":
                await _messageService.SendFeedbackMenuAsync(message.Chat.Id);
                user = await _userService.GetUserByChatIdAsync(message.Chat.Id);
                user.MenuState = MenuState.Feedback;
                await _userService.UpdateUserAsync(user);
                break;

            case "cancel":
                user = await _userService.GetUserByChatIdAsync(message.Chat.Id);
                await HandleCancelOptionAsync(user);
                break;

            default:
                user = await _userService.GetUserByChatIdAsync(message.Chat.Id);
                if (user.MenuState == MenuState.Feedback)
                {
                    await _messageService.SendTextMessageAsync(message.Chat.Id, "Thanks!");
                    await HandleCancelOptionAsync(user);
                    var feedback = $"Date: {message.Date}{Environment.NewLine}" +
                                   $"From: {message.From.FirstName} {message.From.LastName}, @{message.From.Username}{Environment.NewLine}" +
                                   $"BotVersion: v{GetBotVersion()}{Environment.NewLine}" +
                                   $"ChatId: {message.Chat.Id}{Environment.NewLine}" +
                                   $"Message: {message.Text}";
                    
                     await _userService.SendFeedbackAsync(feedback);
                    return;
                }

                var chatInfo = await _chatService.GetChatInfoAsync(message.Chat.Id, message.MessageId - 2);
                if (chatInfo is not null && chatInfo.ChatState == ChatState.SearchBooks)
                {
                    chatInfo.SearchQuery = message.Text.Trim();
                    await _chatService.SaveChatInfoAsync(chatInfo);
                    var searchBooks = await GetBookDataResponses(chatInfo.PageNumber, chatInfo);
                    if (searchBooks.Count != 0)
                        await _messageService.DisplayBookButtons(chatInfo.ChatId, "This is the result of your search query.", searchBooks, chatInfo.ChatState);
                    else
                        await _messageService.DisplayBookButtons(chatInfo.ChatId, "There are no such books in our library.", searchBooks, chatInfo.ChatState);
                }
                else
                    await _messageService.SendWelcomeMessageAsync(message.Chat.Id);
                break;
        }
    }

    private async Task HandleShowFilteredOptionAsync(Message message)
    {
        var previousBotMessageId = message.MessageId - 1;
        ChatDbModel data = await _chatService.GetChatInfoAsync(message.Chat.Id, previousBotMessageId);

        if (data is null || data.ChatState != ChatState.Filters)
        {
            await _messageService.SendTextMessageAsync(message.Chat.Id, "Sorry, we can't find message with filters");
            var user = await _userService.GetUserByChatIdAsync(message.Chat.Id);
            await HandleCancelOptionAsync(user);
            return;
        }

        var allBooks = await GetBookDataResponses(data.PageNumber, data);
        await _messageService.DisplayBookButtons(data.ChatId, $"These books are in our library.{Environment.NewLine}" 
            + GetFiltersAsAStringMessage(data.Filters), allBooks, data.ChatState);
        var chatInfoAllBooks = new ChatDbModel(message.Chat.Id, message.MessageId + 1, ChatState.AllBooks)
        {
            Filters = data.Filters
        };

        await _chatService.SaveChatInfoAsync(chatInfoAllBooks);
    }

    private async Task HandleFilterByPathOptionAsync(Message message, int messageId)
    {
        var chatInfoFilteredBooks = new ChatDbModel(message.Chat.Id, messageId, ChatState.Filters);
        var bookPaths = await _sharePointService.GetBookPathsAsync();
        await _messageService.SendFilterBooksMessageWithInlineKeyboardAsync(chatInfoFilteredBooks.ChatId, "Choose paths for books", bookPaths);
        await _chatService.SaveChatInfoAsync(chatInfoFilteredBooks);
        var user = await _userService.GetUserByChatIdAsync(message.Chat.Id);
        user.MenuState = MenuState.FilteredBooks;
        await _userService.UpdateUserAsync(user);
    }

    private async Task HandleCancelOptionAsync(UserDbModel user)
    {
        switch (user.MenuState)
        {
            case MenuState.None:
            case MenuState.Library:
            case MenuState.Help:
            case MenuState.MyBooks:
                user.MenuState = MenuState.None;
                await _messageService.SendWelcomeMessageAsync(user.ChatId);
                break;
            case MenuState.SearchBooks:
            case MenuState.AllBooks:
            case MenuState.FilteredBooks:
                user.MenuState = MenuState.Library;
                await _messageService.SendLibraryMenuMessageAsync(user.ChatId);
                break;
            case MenuState.Feedback:
                user.MenuState = MenuState.Help;
                await _messageService.SendHelpMenuAsync(user.ChatId);
                break;
            default:
                break;
        }

        await _userService.UpdateUserAsync(user);
    }

    private async Task BotOnCallbackQueryReceived(CallbackQuery callbackQuery)
    {
        var data = await _chatService.GetChatInfoAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId);

        data = data is null ? await _chatService.GetChatInfoAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId - 3) : data;

        bool firstPage = data.PageNumber == 0;
        
        switch (callbackQuery.Data.ToLower())
        {
            case "show all books":
                List<BookDataResponse> allBooks = await GetBookDataResponses(data.PageNumber, data);
                await _messageService.UpdateBookButtonsAndMessageTextAsync(data.ChatId, data.MessageId,
                    $"These books are in our library.{Environment.NewLine}" + GetFiltersAsAStringMessage(data.Filters), allBooks, firstPage, ChatState.AllBooks);
                data.ChatState = ChatState.AllBooks;
                await _chatService.UpdateChatInfoAsync(data);
                break;

            case "next":
                if (data.ChatState == ChatState.AllBooks)
                {
                    var books = await GetBookDataResponses(data.PageNumber + 1, data);
                    if (books.Count != 0)
                    {
                        data.PageNumber++;
                        firstPage = false;
                        await _chatService.UpdateChatInfoAsync(data);
                        await UpdateInlineButtonsAsync(callbackQuery, books, firstPage, data.ChatState);
                    }
                }
                if (data.ChatState == ChatState.SearchBooks)
                {
                    var books = await GetBookDataResponses(data.PageNumber + 1, data);
                    if (books.Count != 0)
                    {
                        data.PageNumber++;
                        firstPage = false;
                        await _chatService.UpdateChatInfoAsync(data);
                        await UpdateInlineButtonsAsync(callbackQuery, books, firstPage, data.ChatState);
                    }
                }
                break;

            case "previous":
                if (data.ChatState == ChatState.AllBooks)
                {
                    if (data.PageNumber - 1 >= 0)
                    {
                        if (data.PageNumber - 1 == 0)
                            firstPage = true;
                        var previousBooks = await GetBookDataResponses(--data.PageNumber, data);
                        await _chatService.UpdateChatInfoAsync(data);
                        await UpdateInlineButtonsAsync(callbackQuery, previousBooks, firstPage, data.ChatState);
                    }
                }
                if (data.ChatState == ChatState.SearchBooks)
                {
                    if (data.PageNumber - 1 >= 0)
                    {
                        if (data.PageNumber - 1 == 0)
                            firstPage = true;
                        var previousBooks = await GetBookDataResponses(--data.PageNumber, data);
                        await _chatService.UpdateChatInfoAsync(data);
                        await UpdateInlineButtonsAsync(callbackQuery, previousBooks, firstPage, data.ChatState);
                    }
                }
                break;

            case "no":
                if (data.ChatState == ChatState.AllBooks)
                {
                    var booksAfterNo = await GetBookDataResponses(data.PageNumber, data);
                    await _messageService.EditMessageAfterYesAndNoButtonsAsync(callbackQuery, $"These books are in our library.{Environment.NewLine}" + GetFiltersAsAStringMessage(data.Filters));
                    await UpdateInlineButtonsAsync(callbackQuery, booksAfterNo, firstPage, data.ChatState);
                }
                if (data.ChatState == ChatState.UserBooks)
                {
                    var userForNo = await _userService.GetUserByChatIdAsync(data.ChatId);
                    var booksAfterNo = await _sharePointService.GetBooksAsync(data.PageNumber, userForNo.SharePointId);
                    var dataAboutBook = await _sharePointService.GetDataAboutBookAsync(data.BookId);
                    var returnDate = dataAboutBook.TakenToRead.Value.AddMonths(2).ToLocalTime().ToShortDateString();
                    await _messageService.EditMessageAfterYesAndNoButtonsAsync(callbackQuery, "Return till " + returnDate);
                    var books = booksAfterNo.Where(book => book.TakenToRead.Value.ToLocalTime().ToShortDateString() == dataAboutBook.TakenToRead.Value.ToShortDateString()).ToList();
                    await UpdateInlineButtonsAsync(callbackQuery, books, true, data.ChatState);
                }
                if (data.ChatState == ChatState.SearchBooks)
                {
                    var booksAfterNo = await GetBookDataResponses(data.PageNumber, data);
                    await _messageService.EditMessageAfterYesAndNoButtonsAsync(callbackQuery, "This is the result of your search query.");
                    await UpdateInlineButtonsAsync(callbackQuery, booksAfterNo, firstPage, data.ChatState);
                }
                break;

            case "yes":
                var user = await _userService.GetUserByChatIdAsync(callbackQuery.Message.Chat.Id);
                if (data.ChatState == ChatState.AllBooks)
                {
                    ChangeBookStatusRequest borrowBook = new ChangeBookStatusRequest(user.SharePointId, user.SharePointId, DateTime.UtcNow, DateTime.UtcNow);
                    List<BookDataResponse> allBooksAfterYes;
                    var dataAboutBook = await _sharePointService.GetDataAboutBookAsync(data.BookId);
                    if (!dataAboutBook.IsBorrowedBook)
                        {
                            await _sharePointService.ChangeBookStatus(callbackQuery.Message.Chat.Id, data.BookId, borrowBook);
                            await _messageService.AnswerCallbackQueryAsync(callbackQuery.Id, $"The book {dataAboutBook.Title} was successfully borrowed!");
                        }
                    else
                        {
                            await _messageService.AnswerCallbackQueryAsync(callbackQuery.Id, $"Something went wrong. The book {dataAboutBook.Title} is already borrowed.");
                        }

                    allBooksAfterYes = await UpdateBooksLibrary(callbackQuery, data);
                    await _messageService.EditMessageAfterYesAndNoButtonsAsync(callbackQuery, $"These books are in our library.{Environment.NewLine}" + GetFiltersAsAStringMessage(data.Filters));
                    await UpdateInlineButtonsAsync(callbackQuery, allBooksAfterYes, firstPage, data.ChatState);
                }

                if (data.ChatState == ChatState.UserBooks)
                {
                    ChangeBookStatusRequest returnBook = new ChangeBookStatusRequest(null, user.SharePointId, null, DateTime.UtcNow);

                    var dataAboutBook = await _sharePointService.GetDataAboutBookAsync(data.BookId);
                    if (dataAboutBook.IsBorrowedBook)
                    {
                        await _sharePointService.ChangeBookStatus(callbackQuery.Message.Chat.Id, data.BookId, returnBook);
                        await _messageService.AnswerCallbackQueryAsync(callbackQuery.Id, $"The book {dataAboutBook.Title} was successfully returned!");
                    }
                    else
                    {
                        await _messageService.AnswerCallbackQueryAsync(callbackQuery.Id, $"Something went wrong. The book '{dataAboutBook.Title}' is already returned.");
                    }

                    var userBooksAfterYes = await UpdateBooksLibrary(callbackQuery, data);
                    if (userBooksAfterYes.Count == 0)
                    {
                        await _messageService.EditMessageAfterYesAndNoButtonsAsync(callbackQuery, "You don't read any books now");
                    }
                    else
                    {
                        var borrowedDate = dataAboutBook.TakenToRead.Value.AddMonths(2).ToLocalTime().ToShortDateString();
                        await _messageService.EditMessageAfterYesAndNoButtonsAsync(callbackQuery, "Return till " + borrowedDate);
                        var books = userBooksAfterYes.Where(book => book.TakenToRead.Value.ToLocalTime().ToShortDateString() == dataAboutBook.TakenToRead.Value.ToShortDateString()).ToList();
                        await UpdateInlineButtonsAsync(callbackQuery, books, true, data.ChatState);
                    }
                }

                if (data.ChatState == ChatState.SearchBooks)
                {
                    ChangeBookStatusRequest borrowBook = new ChangeBookStatusRequest(user.SharePointId, user.SharePointId, DateTime.UtcNow, DateTime.UtcNow);

                    var dataAboutBook = await _sharePointService.GetDataAboutBookAsync(data.BookId);
                    if (!dataAboutBook.IsBorrowedBook)
                    {
                        await _sharePointService.ChangeBookStatus(callbackQuery.Message.Chat.Id, data.BookId, borrowBook);
                        await _messageService.AnswerCallbackQueryAsync(callbackQuery.Id, $"The book {dataAboutBook.Title} was successfully borrowed!");
                    }
                    else
                    {
                        await _messageService.AnswerCallbackQueryAsync(callbackQuery.Id, $"Something went wrong. The book {dataAboutBook.Title} is already borrowed.");
                    }

                    var searchBooksAfterYes = await UpdateBooksLibrary(callbackQuery, data);
                    await _messageService.EditMessageAfterYesAndNoButtonsAsync(callbackQuery, "This is the result of your search query.");
                    await UpdateInlineButtonsAsync(callbackQuery, searchBooksAfterYes, firstPage, data.ChatState);
                }

                break;


            default:
                if (data.ChatState == ChatState.AllBooks)
                {
                    await _messageService.CreateYesAndNoButtonsAsync(callbackQuery, "Are you sure you want to borrow this book?");
                    data.BookId = int.Parse(callbackQuery.Data);
                    await _chatService.UpdateChatInfoAsync(data);
                }
                if (data.ChatState == ChatState.UserBooks)
                {
                    await _messageService.CreateYesAndNoButtonsAsync(callbackQuery, "Are are sure you want to return this book?");
                    data.BookId = int.Parse(callbackQuery.Data);
                    await _chatService.UpdateChatInfoAsync(data);
                }
                if (data.ChatState == ChatState.SearchBooks)
                {
                    await _messageService.CreateYesAndNoButtonsAsync(callbackQuery, "Are you sure you want to borrow this book?");
                    data.BookId = int.Parse(callbackQuery.Data);
                    await _chatService.UpdateChatInfoAsync(data);
                }
                if (data.ChatState == ChatState.Filters)
                {
                    data.Filters = data.Filters is null ? new List<string>() : data.Filters;
                    data.Filters.Add(callbackQuery.Data);
                    await _chatService.UpdateChatInfoAsync(data);
                    var filters = await _sharePointService.GetBookPathsAsync();
                    filters = filters.Except(data.Filters).ToArray();

                    await _messageService.UpdateFilterBooksMessageWithInlineKeyboardAsync(data.ChatId, data.MessageId, filters.ToArray(),
                        $"Choose paths for books.{Environment.NewLine}" + GetFiltersAsAStringMessage(data.Filters));
                }

                break;
                
              case "tilldata":
                break;
        }
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

    private string GetFiltersAsAStringMessage(IEnumerable<string> filters) => filters is null ? string.Empty : $"Your filters: {string.Join(", ", filters)}";

    private async Task<List<BookDataResponse>> GetBookDataResponses(int pageNumber, ChatDbModel data)
    {
        if (data.Filters is not null && data.Filters.Count > 0)
        {
            return await _sharePointService.GetBooksAsync(pageNumber, data.Filters);
        }

        if (!string.IsNullOrWhiteSpace(data.SearchQuery))
        {
            return await _sharePointService.GetBooksAsync(pageNumber, data.SearchQuery);
        }

        return await _sharePointService.GetBooksAsync(pageNumber);
    }
    private async Task UpdateInlineButtonsAsync(CallbackQuery callbackQuery, List<BookDataResponse> books, bool firstPage, ChatState chatState)
    {
        if (chatState == ChatState.UserBooks)
            await _messageService.UpdateUserBookButtonsAsync(callbackQuery.Message, books);
        else
            await _messageService.UpdateBookButtons(callbackQuery.Message, books, firstPage, chatState);
    }

    private async Task<List<BookDataResponse>> UpdateBooksLibrary(CallbackQuery callbackQuery, ChatDbModel data)
    {
        await _sharePointService.UpdateBooksData();
        if (data.ChatState != ChatState.UserBooks)
        {
            return await GetBookDataResponses(data.PageNumber, data);
        }
        else
        {
            var userForNext = await _userService.GetUserByChatIdAsync(callbackQuery.Message.Chat.Id);
            return await _sharePointService.GetBooksAsync(data.PageNumber, userForNext.SharePointId);
        }
    }

    private Task UnknownUpdateHandlerAsync(Update update)
    {
        return Task.CompletedTask;
    }

    private string GetBotVersion()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
        return fileVersionInfo.FileVersion;
    }
}
