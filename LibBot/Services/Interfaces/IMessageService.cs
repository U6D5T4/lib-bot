using LibBot.Models.SharePointResponses;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace LibBot.Services.Interfaces;

public interface IMessageService
{
    public Task<Message> SayHelloFromAntonAsync(ITelegramBotClient bot, Message message); 
    public Task<Message> SendTextMessageAndClearKeyboardAsync(ITelegramBotClient bot, long chatId, string message);
    public Task<Message> SayHelloFromArtyomAsync(ITelegramBotClient bot, Message message);
    public Task<Message> AskToEnterOutlookLoginAsync(ITelegramBotClient bot, Message message);
    public Task<Message> AskToEnterAuthCodeAsync(ITelegramBotClient bot, Message message);
    public Task<Message> SayDefaultMessageAsync(ITelegramBotClient bot, Message message);
    public InlineKeyboardMarkup SetInlineKeyboardInTwoColumns(List<InlineKeyboardButton> buttons);
    public Task CreateYesAndNoButtons(ITelegramBotClient bot, CallbackQuery callbackQuery);
    public List<InlineKeyboardButton> CreateBookButtons(List<BookDataResponse> books);
    public Task<Message> DisplayBookButtons(ITelegramBotClient bot, Message message, List<BookDataResponse> books);
    public Task UpdateBookButtons(Message message, List<BookDataResponse> books);
    public Task EditMessageAfterYesAndNoButtons(ITelegramBotClient bot, CallbackQuery callbackQuery);
}
