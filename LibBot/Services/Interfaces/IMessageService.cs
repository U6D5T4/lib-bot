using LibBot.Models.SharePointResponses;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace LibBot.Services.Interfaces;

public interface IMessageService
{
    Task<Message> SendTextMessageAndClearKeyboardAsync(ITelegramBotClient bot, long chatId, string message);
    Task<Message> AskToEnterOutlookLoginAsync(ITelegramBotClient bot, Message message);
    Task<Message> AskToEnterAuthCodeAsync(ITelegramBotClient bot, Message message);
    Task<Message> SendWelcomeMessageAsync(long chatId);
    Task<Message> AksToEnterSearchQueryAsync(ITelegramBotClient bot, Message message);
    Task<Message> SayThisBookIsAlreadyBorrowAsync(ITelegramBotClient bot, Message message);
    Task CreateYesAndNoButtons(CallbackQuery callbackQuery, string message);
    List<InlineKeyboardButton> CreateBookButtons(List<BookDataResponse> books, bool firstPage);
    Task<Message> DisplayBookButtons(long chatId, string messageText, List<BookDataResponse> books);
    Task UpdateBookButtons(Message message, List<BookDataResponse> books, bool firstPage);
    Task UpdateBookButtonsAndMessageText(long chatId, int messageId, string messageText, List<BookDataResponse> books, bool firstPage);
    Task EditMessageAfterYesAndNoButtons(ITelegramBotClient bot, CallbackQuery callbackQuery, string messageText);
    Task DisplayInlineButtonsWithMessage(Message message, string messageText, params string[] buttons);
    Task UpdateInlineButtonsWithMessage(long chatId, int messageId, string messageText, string[] bookPaths);
    Task SendLibraryMenuMessageAsync(long chatId);
    Task SendMessageWithInlineKeyboardAsync(long chatId, string message, string[] keys);
}
