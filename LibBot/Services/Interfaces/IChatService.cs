using LibBot.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LibBot.Services.Interfaces;

public interface IChatService
{
    Task SaveChatInfoAsync(ChatDbModel chatDbModel);
    Task UpdateChatInfoAsync(ChatDbModel chatDbModel);
    Task<ChatDbModel> GetChatInfoAsync(long chatId, int inlineMessageId);
    Task DeleteChatInfoAsync(long chatId, int inlineMessageId);
    Task<IEnumerable<ChatDbModel>> GetUserChatsInfoAsync(long chatId);
}
