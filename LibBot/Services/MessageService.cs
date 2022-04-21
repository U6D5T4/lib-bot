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

        public async Task<Message> SayHelloFromAnton(ITelegramBotClient bot, Message message)
        {
            return await _botClient.SendTextMessageAsync(message.Chat.Id, "Hello, this is Anton's function!", replyMarkup: replyKeyboardMarkup);
        }
        public async Task<Message> SayHelloFromArtyom(ITelegramBotClient bot, Message message)
        {
            return await _botClient.SendTextMessageAsync(message.Chat.Id, "Hello, this is Artyom's function!", replyMarkup: replyKeyboardMarkup);
        }
        public async Task<Message> SayDefaultMessage(ITelegramBotClient bot, Message message)
        {
            return await _botClient.SendTextMessageAsync(message.Chat.Id, message.Text, replyMarkup: replyKeyboardMarkup);
        }

        public async Task<Message> AskToEnterEmailOrUsername(ITelegramBotClient bot, Message message)
        {
            return await _botClient.SendTextMessageAsync(message.Chat.Id,
                "Please, enter your outlook email or outlook login.", replyMarkup: new ReplyKeyboardRemove());
        }

        public async Task<Message> AskToEnterAuthTokenFromMail(ITelegramBotClient bot, Message message)
        {
            return await _botClient.SendTextMessageAsync(message.Chat.Id,
                "Please, enter authorization token from your email to confirm registration.", replyMarkup: new ReplyKeyboardRemove());
        }
    }
}
