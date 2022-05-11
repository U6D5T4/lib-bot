using LibBot.Models;
using LibBot.Models.SharePointResponses;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace LibBot.Services.Interfaces;

public interface IMessageService
{
    Task<Message> SendTextMessageAndClearKeyboardAsync(long chatId, string message);
    Task<Message> AskToEnterOutlookLoginAsync(Message message);
    Task<Message> AskToEnterAuthCodeAsync(Message message);
    Task<Message> SendWelcomeMessageAsync(long chatId);
    Task<Message> AksToEnterSearchQueryAsync(Message message);
    Task<Message> SayThisBookIsAlreadyBorrowAsync(Message message);
    Task CreateYesAndNoButtonsAsync(CallbackQuery callbackQuery, string message);
    List<InlineKeyboardButton> CreateBookButtonsAsync(List<BookDataResponse> books, bool firstPage);
    List<InlineKeyboardButton> CreateUserBookButtonsAsync(List<BookDataResponse> books);
    Task<Message> DisplayBookButtons(long chatId, string messageText, List<BookDataResponse> books, ChatState chatState);
    Task UpdateBookButtons(Message message, List<BookDataResponse> books, bool firstPage, ChatState chatState);
    Task UpdateBookButtonsAndMessageTextAsync(long chatId, int messageId, string messageText, List<BookDataResponse> books, bool firstPage, ChatState chatState);
    Task EditMessageAfterYesAndNoButtonsAsync(CallbackQuery callbackQuery, string messageText);
    Task DisplayInlineButtonsWithMessageAsync(Message message, string messageText, params string[] buttons);
    Task UpdateFilterBooksMessageWithInlineKeyboardAsync(long chatId, int messageId, string[] bookPaths, string message = null);
    Task SendFilterBooksMessageWithInlineKeyboardAsync(long chatId, string message, string[] bookPaths);
    Task SendLibraryMenuMessageAsync(long chatId);
    Task SendFilterMenuMessageWithKeyboardAsync(long chatId);
    Task SendTextMessageAsync(long chatId, string message);
}
