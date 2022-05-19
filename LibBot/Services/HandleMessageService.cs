using LibBot.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace LibBot.Services;

public partial class HandleUpdateService
{
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
                    var returnDateDistinct = myBooks.Select(book => book.TakenToRead.Value.ToShortDateString()).Distinct();
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
                var messageText = newBooks.Count != 0 ? _resourceReader.GetString("BooksLibraryNew") : _resourceReader.GetString("EmptyBooksLibraryNew");
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
    private string GetBotVersion()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
        return fileVersionInfo.FileVersion;
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
        var resourceName = allBooks.Count != 0 ? "BooksLibrary" : "EmptyLibrary";
        await _messageService.DisplayBookButtons(data.ChatId, _resourceReader.GetString(resourceName) + $"{Environment.NewLine}"
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
            await _messageService.SendTextMessageAsync(message.Chat.Id, _resourceReader.GetString("NoBookInHistory"));
        }
        else
        {
            var booksInfo = new List<string>();
            for (int i = 0; i < user.BorrowedBooks.Count; i++)
            {
                var bookTitle = string.IsNullOrEmpty(user.BorrowedBooks[i].Title) ? string.Empty : $"'{user.BorrowedBooks[i].Title}'.";
                var takenToRead = string.Format(_resourceReader.GetString("TakenToReadBookDate"), user.BorrowedBooks[i].TakenToRead.ToShortDateString());
                var returned = user.BorrowedBooks[i].Returned < user.BorrowedBooks[i].TakenToRead ? string.Empty : string.Format(_resourceReader.GetString("ReturnBookDate"), user.BorrowedBooks[i].Returned.ToShortDateString());
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

}
