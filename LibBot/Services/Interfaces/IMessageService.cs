using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace LibBot.Services.Interfaces;

public interface IMessageService
{
    public Task<Message> SayHelloFromAnton(ITelegramBotClient bot, Message message);
    public Task<Message> SayHelloFromArtyom(ITelegramBotClient bot, Message message);
    public Task<Message> SayDefaultMessage(ITelegramBotClient bot, Message message);
    public Task<Message> AskToEnterEmailOrUsername(ITelegramBotClient bot, Message message);
    public Task<Message> AskToEnterAuthTokenFromMail(ITelegramBotClient bot, Message message);
}
