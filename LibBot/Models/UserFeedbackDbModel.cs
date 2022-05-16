using System;

namespace LibBot.Models;

public class UserFeedbackDbModel
{
    public long ChatId { get; set; }
    public string Name { get; set; }
    public string Username { get; set; }
    public DateTime Date { get; set; }
    public string BotVersion { get; set; }
    public string Message { get; set; }
}
