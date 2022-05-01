using LibBot.Models;
using System.Threading.Tasks;

namespace LibBot.Services.Interfaces;

public interface IChatService
{
    public Task SaveChatInfoAsync(ChatDbModel chatDbModel);
    public Task UpdateChatInfoAsync(ChatDbModel chatDbModel);
    public Task<ChatDbModel> GetChatInfoAsync(long chatId, int inlineMessageId);
    public Task DeleteChatInfoAsync(long chatId, int inlineMessageId);
}
