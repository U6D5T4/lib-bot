namespace LibBot.Models;

public interface IUserDbModel
{
    public int ChatId { get; set; }
    public int SharePointId { get; set; }
    public bool IsConfirmed { get; set; }
}
