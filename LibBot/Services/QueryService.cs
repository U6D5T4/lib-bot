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
    public async Task DeleteChatInfoAsync(long chatId)
    {
        await _queryDbService.DeleteItemAsync(chatId);
    }

    public async Task<string> GetQueryAsync(long chatId)
    {
       return await _queryDbService.ReadItemAsync(chatId);
    }

    public async Task SaveQueryAsync(long chatId, string query)
    {
        await _queryDbService.CreateItemAsync(chatId, query);
    }

    public async Task UpdateQueryAsync(long chatId, string query)
    {
        await _queryDbService.UpdateItemAsync(chatId, query);
    }
}
