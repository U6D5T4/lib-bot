using LibBot.Models;
using System.Threading.Tasks;

namespace LibBot.Services.Interfaces;

public interface IChatDbService
{
    public Task CreateItemAsync(ChatDbModel item);
    public Task<ChatDbModel> ReadItemAsync(long chatId, long inlineMessageId);
    public Task UpdateItemAsync(ChatDbModel item);
    public Task DeleteItemAsync(long chatId, long inlineMessageId);
}
