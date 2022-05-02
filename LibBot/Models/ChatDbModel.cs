namespace LibBot.Models;

public class ChatDbModel
{
    public long ChatId { get; set; }
    public int InlineMessageId { get; set; }
    public ChatState ChatState { get; set; }
    public int PageNumber { get; set; }
    public int BookId { get; set; }

}
