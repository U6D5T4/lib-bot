using LibBot.Services.Interfaces;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace LibBot.Services
{
    public class MessageService : IMessageService
    {
        private readonly ITelegramBotClient _botClient;

        public MessageService(ITelegramBotClient botClient)
        {
            _botClient = botClient;
        }

    private ReplyKeyboardMarkup replyKeyboardMarkup = new(
        new[]
        {
            new KeyboardButton[] { "/first", "/second" },
        })
    {
        ResizeKeyboard = true
    };

        public async Task<Message> SayHelloFromAntonAsync(ITelegramBotClient bot, Message message)
        {
            return await _botClient.SendTextMessageAsync(message.Chat.Id, "Hello, this is Anton's function!", replyMarkup: replyKeyboardMarkup);
        }

        public async Task<Message> SendTextMessageAndClearKeyboardAsync(ITelegramBotClient bot, long chatId, string message)
        {
            return await _botClient.SendTextMessageAsync(chatId, message, replyMarkup: new ReplyKeyboardRemove());
        }

        public async Task<Message> SayHelloFromArtyomAsync(ITelegramBotClient bot, Message message)
        {
            return await _botClient.SendTextMessageAsync(message.Chat.Id, "Hello, this is Artyom's function!", replyMarkup: replyKeyboardMarkup);
        }
        public async Task<Message> SayDefaultMessageAsync(ITelegramBotClient bot, Message message)
        {
            return await _botClient.SendTextMessageAsync(message.Chat.Id, "Hey, I'm LibBot. If you are seeing this message, You have completed authentication successfully!", replyMarkup: replyKeyboardMarkup);
        }
    }
}
