using LibBot.Services.Interfaces;
using System.Threading.Tasks;

namespace LibBot.Services;

public class QueryService : IQueryService
{
    private readonly IQueryDbService _queryDbService;
    public QueryService(IQueryDbService queryDbService)
    {
        _queryDbService = queryDbService;
    }
    public async Task DeleteChatInfoAsync(long chatId, long messageId)
    {
        await _queryDbService.DeleteItemAsync(chatId, messageId);
    }

    public async Task<string> GetQueryAsync(long chatId, long messageId)
    {
       return await _queryDbService.ReadItemAsync(chatId, messageId);
    }

    public async Task SaveQueryAsync(long chatId, long messageId, string query)
    {
        await _queryDbService.CreateItemAsync(chatId, messageId, query);
    }

    public async Task UpdateQueryAsync(long chatId, long messageId, string query)
    {
        await _queryDbService.UpdateItemAsync(chatId, messageId, query);
    }
}
