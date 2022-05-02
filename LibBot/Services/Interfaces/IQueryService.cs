using System.Threading.Tasks;

namespace LibBot.Services;

public interface IQueryService
{
    public Task SaveQueryAsync(long chatId, string query);
    public Task UpdateQueryAsync(long chatId, string query);
    public Task<string> GetQueryAsync(long chatId);
    public Task DeleteChatInfoAsync(long chatId);
}
