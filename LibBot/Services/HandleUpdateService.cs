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
using System.Resources;

namespace LibBot.Services;

public class HandleUpdateService : IHandleUpdateService
{
    private static readonly NLog.Logger _logger;
    private ResourceManager _resourceReader;
    static HandleUpdateService() => _logger = NLog.LogManager.GetCurrentClassLogger();

    private readonly IMessageService _messageService;
    private readonly IUserService _userService;
    private readonly ISharePointService _sharePointService;
    private readonly IChatService _chatService;
    private readonly IFeedbackService _feedbackService;

    public HandleUpdateService(IMessageService messageService, IUserService userService, ISharePointService sharePointService, IChatService chatService, IFeedbackService feedbackService)
    {
        _messageService = messageService;
        _userService = userService;
        _sharePointService = sharePointService;
        _chatService = chatService;
        _feedbackService = feedbackService;
        _resourceReader = new ResourceManager("LibBot.Resources.Resource", Assembly.GetExecutingAssembly());
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
            await _messageService.SendTextMessageAndClearKeyboardAsync(chatId, _resourceReader.GetString("ExpiredCode"));
            await _messageService.AskToEnterAuthCodeAsync(message);
            return false;
        }

        if (!await _userService.VerifyAccountAsync(message.Text, chatId))
        {
            await _messageService.SendTextMessageAndClearKeyboardAsync(chatId, _resourceReader.GetString("WrongCode"));
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
            await _messageService.SendTextMessageAndClearKeyboardAsync(message.Chat.Id, _resourceReader.GetString("WrongCredentials"));
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
               _resourceReader.GetString("WrongSendCode"));
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
                await DeletePreviousMessageAsync(message.Chat.Id);
                var chatInfoAllBooks = new ChatDbModel(message.Chat.Id, new List<int>() { message.MessageId + 1 }, ChatState.AllBooks);
                var allBooks = await GetBookDataResponses(chatInfoAllBooks.PageNumber, chatInfoAllBooks);
                await _messageService.DisplayBookButtons(chatInfoAllBooks.ChatId,
                     _resourceReader.GetString("BooksLibrary"), allBooks, chatInfoAllBooks.ChatState);
                await _chatService.SaveChatInfoAsync(chatInfoAllBooks);
                break;

            case "my books":
                var chatInfoUserBooks = new ChatDbModel(message.Chat.Id, new List<int>(), ChatState.UserBooks);
                user = await _userService.GetUserByChatIdAsync(message.Chat.Id);
                var myBooks = await _sharePointService.GetBooksAsync(0, user.SharePointId);
                if (myBooks.Count != 0)
                {
                    await DeletePreviousMessageAsync(message.Chat.Id);
                    await _messageService.CreateUserBookButtonsAsync(chatInfoUserBooks.ChatId, myBooks);
                    var returnDateDistinct = myBooks.Select(book => book.TakenToRead.Value.AddMonths(3).ToShortDateString()).Distinct();
                    for (int i = 1; i <= returnDateDistinct.Count(); i++)
                    {
                        chatInfoUserBooks.CurrentMessagesId.Add(message.MessageId + i);
                    }
                }
                else
                {
                    await _messageService.DisplayBookButtons(chatInfoUserBooks.ChatId, _resourceReader.GetString("EmptyUserLibrary"), myBooks, chatInfoUserBooks.ChatState);
                    chatInfoUserBooks.CurrentMessagesId.Add(message.MessageId);
                }

                await _chatService.SaveChatInfoAsync(chatInfoUserBooks);
                break;

            case "search books":
                await _messageService.AksToEnterSearchQueryAsync(message);
                user = await _userService.GetUserByChatIdAsync(message.Chat.Id);
                user.MenuState = MenuState.SearchBooks;
                await _userService.UpdateUserAsync(user);
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

            case "history":
                await HandleHistoryOption(message);
                break;

            case "new arrivals":
                await DeletePreviousMessageAsync(message.Chat.Id);
                var chatInfoNewBooks = new ChatDbModel(message.Chat.Id, new List<int> { message.MessageId + 1 }, ChatState.NewArrivals);
                var newBooks = await _sharePointService.GetNewBooksAsync(chatInfoNewBooks.PageNumber);
                var messageText = newBooks.Count != 0 ? "These are new books in our library" : "There are no new books in our library";
                await _messageService.DisplayBookButtons(chatInfoNewBooks.ChatId,
                    messageText, newBooks, chatInfoNewBooks.ChatState);
                await _chatService.SaveChatInfoAsync(chatInfoNewBooks);
                break;

            default:
                user = await _userService.GetUserByChatIdAsync(message.Chat.Id);
                if (user.MenuState == MenuState.Feedback)
                {
                    await _messageService.SendTextMessageAsync(message.Chat.Id, _resourceReader.GetString("Thanks"));
                    await HandleCancelOptionAsync(user);

                    var feedback = new UserFeedbackDbModel(message, "v" + GetBotVersion());
                    await _feedbackService.SaveFeedbackIntoDb(feedback);
                }
                else if (user.MenuState == MenuState.SearchBooks)
                {
                    await DeletePreviousMessageAsync(message.Chat.Id);
                    var chatInfo = new ChatDbModel(message.Chat.Id, new List<int>() { message.MessageId + 1 }, ChatState.SearchBooks)
                    {
                        SearchQuery = message.Text.Trim()
                    };
                    await _chatService.SaveChatInfoAsync(chatInfo);
                    var searchBooks = await GetBookDataResponses(chatInfo.PageNumber, chatInfo);
                    if (searchBooks.Count != 0)
                        await _messageService.DisplayBookButtons(chatInfo.ChatId, _resourceReader.GetString("SearchQueryResult"), searchBooks, chatInfo.ChatState);
                    else
                        await _messageService.DisplayBookButtons(chatInfo.ChatId, _resourceReader.GetString("EmptyLibrarySearchQuery"), searchBooks, chatInfo.ChatState);

                }
                else
                    await _messageService.SendWelcomeMessageAsync(message.Chat.Id);
                break;
        }
    }

    private async Task HandleShowFilteredOptionAsync(Message message)
    {
        await DeletePreviousMessageAsync(message.Chat.Id);
        ChatDbModel data = await _chatService.GetChatInfoAsync(message.Chat.Id);
        if (data is null || data.ChatState != ChatState.Filters)
        {
            await _messageService.SendTextMessageAsync(message.Chat.Id, _resourceReader.GetString("LostFilters"));
            var user = await _userService.GetUserByChatIdAsync(message.Chat.Id);
            await HandleCancelOptionAsync(user);
            return;
        }

        var allBooks = await GetBookDataResponses(data.PageNumber, data);
        await _messageService.DisplayBookButtons(data.ChatId, _resourceReader.GetString("EmptyLibrary") + $"{Environment.NewLine}"
            + GetFiltersAsAStringMessage(data.Filters), allBooks, data.ChatState);
        var chatInfoAllBooks = new ChatDbModel(message.Chat.Id, new List<int>() { message.MessageId + 1 }, ChatState.AllBooks)
        {
            Filters = data.Filters
        };

        await _chatService.SaveChatInfoAsync(chatInfoAllBooks);
    }

    private async Task HandleFilterByPathOptionAsync(Message message, int messageId)
    {
        await DeletePreviousMessageAsync(message.Chat.Id);
        var chatInfoFilteredBooks = new ChatDbModel(message.Chat.Id, new List<int>() { messageId }, ChatState.Filters);
        var bookPaths = await _sharePointService.GetBookPathsAsync();
        await _messageService.SendFilterBooksMessageWithInlineKeyboardAsync(chatInfoFilteredBooks.ChatId, _resourceReader.GetString("ChooseFilters"), bookPaths);
        await _chatService.SaveChatInfoAsync(chatInfoFilteredBooks);
        var user = await _userService.GetUserByChatIdAsync(message.Chat.Id);
        user.MenuState = MenuState.FilteredBooks;
        await _userService.UpdateUserAsync(user);
    }

    private async Task HandleHistoryOption(Message message)
    {
        var user = await _userService.GetUserByChatIdAsync(message.Chat.Id);
        if (user.BorrowedBooks is null)
        {
            await _messageService.SendTextMessageAsync(message.Chat.Id, "You don't have any book in your history");
        }
        else
        {
            var booksInfo = new List<string>();
            for (int i = 0; i < user.BorrowedBooks.Count; i++)
            {
                var bookTitle = string.IsNullOrEmpty(user.BorrowedBooks[i].Title) ? string.Empty : $"'{user.BorrowedBooks[i].Title}'.";
                var takenToRead = $"Taken to read: {user.BorrowedBooks[i].TakenToRead.ToShortDateString()}.";
                var returned = user.BorrowedBooks[i].Returned < user.BorrowedBooks[i].TakenToRead ? string.Empty : $"Returned: {user.BorrowedBooks[i].Returned.ToShortDateString()}.";
                booksInfo.Add($"{i + 1}. {bookTitle} {takenToRead} {returned}");
            }

            var history = string.Join(Environment.NewLine, booksInfo);
            await _messageService.SendTextMessageAsync(message.Chat.Id, history);
        }
    }

    private async Task DeletePreviousMessageAsync(long chatId)
    {
        var data = await _chatService.GetChatInfoAsync(chatId);
        if (data is not null && data.CurrentMessagesId is not null)
        {
            foreach (var messageId in data.CurrentMessagesId)
            {
                try
                {
                    await _messageService.DeleteMessageAsync(chatId, messageId);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, _resourceReader.GetString("WrongDeleteMessage"));
                }
            }
        }
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
        var data = await _chatService.GetChatInfoAsync(callbackQuery.Message.Chat.Id);

        if (!data.CurrentMessagesId.Contains(callbackQuery.Message.MessageId))
        {
            await _messageService.AnswerCallbackQueryAsync(callbackQuery.Id, _resourceReader.GetString("LostMessage"));
            return;
        }

        bool firstPage = data.PageNumber == 0;

        switch (callbackQuery.Data.ToLower())
        {
            case "next":
                var books = await GetBookDataResponses(data.PageNumber + 1, data);
                if (books.Count != 0)
                {
                    data.PageNumber++;
                    firstPage = false;
                    await _chatService.UpdateChatInfoAsync(data);
                    await UpdateInlineButtonsAsync(callbackQuery, books, firstPage, data.ChatState);
                }
                break;

            case "previous":
                if (data.PageNumber - 1 >= 0)
                {
                    if (data.PageNumber - 1 == 0)
                        firstPage = true;
                    var previousBooks = await GetBookDataResponses(--data.PageNumber, data);
                    await _chatService.UpdateChatInfoAsync(data);
                    await UpdateInlineButtonsAsync(callbackQuery, previousBooks, firstPage, data.ChatState);
                }
                break;

            case "no":
                if (data.ChatState == ChatState.AllBooks || data.ChatState == ChatState.SearchBooks || data.ChatState == ChatState.NewArrivals)
                {
                    var booksAfterNo = await GetBookDataResponses(data.PageNumber, data);
                    var message = string.Empty;
                    if (data.ChatState == ChatState.AllBooks)
                        message = _resourceReader.GetString("BooksLibrary") + $"{Environment.NewLine}" + GetFiltersAsAStringMessage(data.Filters);
                    if (data.ChatState == ChatState.SearchBooks)
                        message = _resourceReader.GetString("SearchQueryResult");
                    if (data.ChatState == ChatState.NewArrivals)
                        message = _resourceReader.GetString("BooksLibraryNew");

                    await _messageService.EditMessageAfterYesAndNoButtonsAsync(callbackQuery, message);
                    await UpdateInlineButtonsAsync(callbackQuery, booksAfterNo, firstPage, data.ChatState);
                }
                else if (data.ChatState == ChatState.UserBooks)
                {
                    var userForNo = await _userService.GetUserByChatIdAsync(data.ChatId);
                    var booksAfterNo = await _sharePointService.GetBooksAsync(data.PageNumber, userForNo.SharePointId);
                    var dataAboutBook = await _sharePointService.GetDataAboutBookAsync(data.BookId);
                    var returnDate = dataAboutBook.TakenToRead.Value.AddMonths(2).ToLocalTime().ToShortDateString();
                    await _messageService.EditMessageAfterYesAndNoButtonsAsync(callbackQuery, string.Format(_resourceReader.GetString("BooksReturnTill"), returnDate));
                    books = booksAfterNo.Where(book => book.TakenToRead.Value.ToLocalTime().ToShortDateString() == dataAboutBook.TakenToRead.Value.ToShortDateString()).ToList();
                    await UpdateInlineButtonsAsync(callbackQuery, books, true, data.ChatState);
                }
                break;

            case "yes":
                var user = await _userService.GetUserByChatIdAsync(callbackQuery.Message.Chat.Id);
                Task updateUserTask = null;
                if (data.ChatState == ChatState.AllBooks || data.ChatState == ChatState.SearchBooks || data.ChatState == ChatState.NewArrivals)
                {
                    ChangeBookStatusRequest changeBookStatus = new ChangeBookStatusRequest(user.SharePointId, user.SharePointId, DateTime.UtcNow, DateTime.UtcNow);

                    var dataAboutBook = await _sharePointService.GetDataAboutBookAsync(data.BookId);
                    await _sharePointService.ChangeBookStatus(callbackQuery.Message.Chat.Id, data.BookId, changeBookStatus);
                    await _messageService.AnswerCallbackQueryAsync(callbackQuery.Id, string.Format(_resourceReader.GetString("SuccessfullyBorrowed"), dataAboutBook.Title));

                    var borrowedBook = new BorrowedBook(data.BookId, changeBookStatus.TakenToRead.Value, dataAboutBook.Title);
                    user.BorrowedBooks = user.BorrowedBooks is null ? new List<BorrowedBook>() : user.BorrowedBooks;
                    user.BorrowedBooks.Add(borrowedBook);
                    updateUserTask = _userService.UpdateUserAsync(user);

                    var updatedBooks = await UpdateBooksLibrary(callbackQuery, data);
                    var message = string.Empty;
                    if (data.ChatState == ChatState.AllBooks)
                        message = _resourceReader.GetString("BooksLibrary") + $"{Environment.NewLine}" + GetFiltersAsAStringMessage(data.Filters);
                    if (data.ChatState == ChatState.SearchBooks)
                        message = _resourceReader.GetString("SearchQueryResult");
                    if (data.ChatState == ChatState.NewArrivals)
                        message = _resourceReader.GetString("BooksLibraryNew");

                    await _messageService.EditMessageAfterYesAndNoButtonsAsync(callbackQuery, message);
                    await UpdateInlineButtonsAsync(callbackQuery, updatedBooks, firstPage, data.ChatState);
                }
                else if (data.ChatState == ChatState.UserBooks)
                {
                    var returnBook = new ChangeBookStatusRequest(null, user.SharePointId, null, DateTime.UtcNow);

                    var dataAboutBook = await _sharePointService.GetDataAboutBookAsync(data.BookId);
                    if (dataAboutBook.IsBorrowedBook)
                    {
                        await _sharePointService.ChangeBookStatus(callbackQuery.Message.Chat.Id, data.BookId, returnBook);
                        await _messageService.AnswerCallbackQueryAsync(callbackQuery.Id, string.Format(_resourceReader.GetString("SuccessfullyReturned"), dataAboutBook.Title));
                        var bookToReturn = user.BorrowedBooks.LastOrDefault(book => book.BookId == data.BookId);
                        if (bookToReturn is not null)
                        {
                            bookToReturn.Returned = returnBook.Modified;
                            updateUserTask = _userService.UpdateUserAsync(user);
                        }
                    }
                    else
                    {
                        await _messageService.AnswerCallbackQueryAsync(callbackQuery.Id, string.Format(_resourceReader.GetString("UnsuccessfullyReturned"), dataAboutBook.Title));
                        _logger.Warn(string.Format(_resourceReader.GetString("LogUnsuccessfullyReturned"), dataAboutBook.Title));
                    }

                    var userBooksAfterYes = await UpdateBooksLibrary(callbackQuery, data);
                    books = userBooksAfterYes.Where(book => book.TakenToRead.Value.ToLocalTime().ToShortDateString() == dataAboutBook.TakenToRead.Value.ToShortDateString()).ToList();
                    if (books.Count == 0)
                    {
                        await _messageService.DeleteMessageAsync(data.ChatId, callbackQuery.Message.MessageId);
                        data.CurrentMessagesId.Remove(callbackQuery.Message.MessageId);
                        await _chatService.SaveChatInfoAsync(data);
                    }
                    else
                    {
                        var returnDate = dataAboutBook.TakenToRead.Value.AddMonths(2).ToLocalTime().ToShortDateString();
                        await _messageService.EditMessageAfterYesAndNoButtonsAsync(callbackQuery, string.Format(_resourceReader.GetString("BooksReturnTill"), returnDate));
                        await UpdateInlineButtonsAsync(callbackQuery, books, true, data.ChatState);
                    }
                }

                if (updateUserTask is not null)
                {
                    await updateUserTask;
                }

                break;

            default:
                if (data.ChatState == ChatState.AllBooks || data.ChatState == ChatState.SearchBooks || data.ChatState == ChatState.NewArrivals)
                {
                    data.BookId = int.Parse(callbackQuery.Data);
                    var dataAboutBook = await _sharePointService.GetDataAboutBookAsync(data.BookId);
                    if (!dataAboutBook.IsBorrowedBook)
                    {
                        await _messageService.CreateYesAndNoButtonsAsync(callbackQuery, string.Format(_resourceReader.GetString("BorrowBookQuestion"), dataAboutBook.Title));
                        await _chatService.UpdateChatInfoAsync(data);
                    }
                    else
                    {
                        await _messageService.AnswerCallbackQueryAsync(callbackQuery.Id, string.Format(_resourceReader.GetString("UnsuccessfullyBorrowed"), dataAboutBook.Title));
                        await _sharePointService.UpdateBooksData();
                    }
                }
                else if (data.ChatState == ChatState.UserBooks)
                {
                    data.BookId = int.Parse(callbackQuery.Data);
                    var dataAboutBook = await _sharePointService.GetDataAboutBookAsync(data.BookId);
                    await _messageService.CreateYesAndNoButtonsAsync(callbackQuery, string.Format(_resourceReader.GetString("ReturnBookQuestion"), dataAboutBook.Title));
                    await _chatService.UpdateChatInfoAsync(data);
                }
                else if (data.ChatState == ChatState.Filters)
                {
                    data.Filters = data.Filters is null ? new List<string>() : data.Filters;
                    if (data.Filters.Contains(callbackQuery.Data))
                    {
                        return;
                    }

                    data.Filters.Add(callbackQuery.Data);
                    await _chatService.UpdateChatInfoAsync(data);
                    var filters = await _sharePointService.GetBookPathsAsync();
                    filters = filters.Except(data.Filters).ToArray();

                    await _messageService.UpdateFilterBooksMessageWithInlineKeyboardAsync(data.ChatId, callbackQuery.Message.MessageId, filters.ToArray(),
                        _resourceReader.GetString("ChooseFilters") + $"{Environment.NewLine}" + GetFiltersAsAStringMessage(data.Filters));
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

        _logger.Error(exception, ErrorMessage);
        return Task.CompletedTask;
    }

    private string GetFiltersAsAStringMessage(IEnumerable<string> filters) => filters is null ? string.Empty : _resourceReader.GetString("UserFilters") + $"{string.Join(", ", filters)}";

    private async Task<List<BookDataResponse>> GetBookDataResponses(int pageNumber, ChatDbModel data)
    {
        if (data.ChatState == ChatState.NewArrivals)
        {
            return await _sharePointService.GetNewBooksAsync(pageNumber);
        }

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
