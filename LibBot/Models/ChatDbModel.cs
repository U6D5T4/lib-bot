using System.Collections.Generic;

namespace LibBot.Models;

public class ChatDbModel
{
    public long ChatId { get; set; }
    public List<int> CurrentMessagesId { get; set; }
    public ChatState ChatState { get; set; }
    public int PageNumber { get; set; }
    public int BookId { get; set; }
    public string SearchQuery { get; set; }
    public List<string> Filters { get; set; }

    public ChatDbModel() { }

    public ChatDbModel(long chatId, List<int> currentMessagesId, ChatState chatState)
    {
        ChatId = chatId;
        CurrentMessagesId = currentMessagesId;
        ChatState = chatState;
        PageNumber = 0;
    }
}


