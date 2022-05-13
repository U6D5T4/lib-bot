using System.Net.Mail;

namespace LibBot.Models;

public class UserDbModel
{
    public long ChatId { get; set; }
    public int SharePointId { get; set; }
    public string Email { get; set; }
    public bool IsConfirmed { get; set; }
    public MenuState MenuState { get; set; }
}
