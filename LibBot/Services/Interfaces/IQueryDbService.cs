using System.Threading.Tasks;

namespace LibBot.Services.Interfaces;

public interface IQueryDbService
{
    public Task CreateItemAsync(long chatId, long messageId, string query);
    public Task UpdateItemAsync(long chatId, long messageId, string query);
    public Task DeleteItemAsync(long chatId, long messageId);
    public Task<string> ReadItemAsync(long chatId, long messageId);

}
