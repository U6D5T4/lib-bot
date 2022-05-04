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

    public async Task CreateItemAsync(long chatId, string query)
    {
        await _client.SetAsync(_dbName + chatId, query);
    }

    public async Task UpdateItemAsync(long chatId, string query)
    {
        await _client.UpdateAsync(_dbName + chatId, query);
    }

    public async Task DeleteItemAsync(long chatId)
    {
        await _client.DeleteAsync(_dbName + chatId);
    }

    public async Task<string> ReadItemAsync(long chatId)
    {
        var result = await _client.GetAsync(_dbName + chatId);
        var data = result.ResultAs<string>();
        return data;
    }
}
