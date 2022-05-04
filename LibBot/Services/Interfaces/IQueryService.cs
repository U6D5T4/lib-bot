using System.Threading.Tasks;

namespace LibBot.Services;

public interface IQueryService
{
    public Task SaveQueryAsync(long chatId, long messageId, string query);
    public Task UpdateQueryAsync(long chatId, long messageId, string query);
    public Task<string> GetQueryAsync(long chatId, long messageId);
    public Task DeleteChatInfoAsync(long chatId, long messageId);
}
