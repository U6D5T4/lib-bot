using FireSharp.Interfaces;
using LibBot.Services.Interfaces;
using System.Threading.Tasks;

namespace LibBot.Services;

public class QueryDbService : IQueryDbService
{
    private readonly IConfigureDb _configureDb;
    private readonly IFirebaseClient _client;
    private const string _dbName = "Queries/";


    public QueryDbService(IConfigureDb configureDb)
    {
        _configureDb = configureDb;
        _client = _configureDb.GetFirebaseClient();
    }

    public async Task CreateItemAsync(long chatId, long messageId, string query)
    {
        await _client.SetAsync(_dbName + chatId + '/' + messageId, query);
    }

    public async Task UpdateItemAsync(long chatId, long messageId, string query)
    {
        await _client.UpdateAsync(_dbName + chatId + '/' + messageId, query);
    }

    public async Task DeleteItemAsync(long chatId,long messageId)
    {
        await _client.DeleteAsync(_dbName + chatId + '/' + messageId);
    }

    public async Task<string> ReadItemAsync(long chatId, long messageId)
    {
        var result = await _client.GetAsync(_dbName + chatId + '/' + messageId);
        var data = result.ResultAs<string>();
        return data;
    }
}
