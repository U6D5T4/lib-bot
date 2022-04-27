using LibBot.Models.SharePointResponses;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace LibBot.Services.Interfaces
{
    public interface IBookService
    {
        public List<InlineKeyboardButton> CreateBookButtons(List<BookDataResponse> books);
        public Task<Message> DisplayBookButtons(ITelegramBotClient bot, Message message, List<BookDataResponse> books);
        public Task UpdateBookButtons(ITelegramBotClient bot, Message message, List<BookDataResponse> books);
    }
}
