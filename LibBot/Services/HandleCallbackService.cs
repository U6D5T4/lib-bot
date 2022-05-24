using LibBot.Models.SharePointRequests;
using LibBot.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using Telegram.Bot.Types;
using LibBot.Models.SharePointResponses;

namespace LibBot.Services;

public partial class HandleUpdateService
{
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
                    await _sharePointService.ChangeBookStatusAsync(callbackQuery.Message.Chat.Id, data.BookId, changeBookStatus);
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
                        await _sharePointService.ChangeBookStatusAsync(callbackQuery.Message.Chat.Id, data.BookId, returnBook);
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
                        if (data.CurrentMessagesId.Count == 1)
                        {
                            await _messageService.EditMessageAfterYesAndNoButtonsAsync(callbackQuery, _resourceReader.GetString("EmptyUserLibrary"));
                        }
                        else
                        {
                            await _messageService.DeleteMessageAsync(data.ChatId, callbackQuery.Message.MessageId);
                            data.CurrentMessagesId.Remove(callbackQuery.Message.MessageId);
                            await _chatService.SaveChatInfoAsync(data);
                        }
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
                        await _sharePointService.UpdateBooksDataAsync();
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
        }
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
        await _sharePointService.UpdateBooksDataAsync();
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
}
