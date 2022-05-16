using System;
using Telegram.Bot.Types;

namespace LibBot.Models;

public class UserFeedbackDbModel
{
    public UserFeedbackDbModel(Message message, string appVersion)
    {
        ChatId = message.Chat.Id;
        Date = message.Date;
        BotVersion = appVersion;
        Name = $"{message.From.FirstName} {message.From.LastName}";
        Username = message.From.Username;
        Message = message.Text;
    }
    public long ChatId { get; set; }
    public string Name { get; set; }
    public string Username { get; set; }
    public DateTime Date { get; set; }
    public string BotVersion { get; set; }
    public string Message { get; set; }
}
