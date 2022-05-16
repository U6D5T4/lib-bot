using System.Collections.Generic;

namespace LibBot.Models;

public class ChatDbModel
{
    public long ChatId { get; set; }
    public int MessageId { get; set; }
    public ChatState ChatState { get; set; }
    public int PageNumber { get; set; }
    public int BookId { get; set; }
    public string SearchQuery { get; set; }
    public List<string> Filters { get; set; }

    public ChatDbModel() { }

    public ChatDbModel(long chatId, int messageId, ChatState chatState)
    {
        ChatId = chatId;
        MessageId = messageId;
        ChatState = chatState;
        PageNumber = 0;
    }
}


