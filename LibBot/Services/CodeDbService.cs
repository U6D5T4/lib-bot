using FireSharp.Interfaces;
using LibBot.Models;
using LibBot.Services.Interfaces;
using System.Threading.Tasks;

namespace LibBot.Services;

public class CodeDbService:ICodeDbService
{
    private readonly IConfigureDb _configureDb;
    private readonly IFirebaseClient _client;
    private static readonly string _dbName = "Codes/";
    public CodeDbService(IConfigureDb configureDb)
    {
        _configureDb = configureDb;
        _client = _configureDb.GetFirebaseClient();
    }

    public async Task CreateItemAsync(CodeDbModel item)
    {
       await _client.SetAsync(_dbName + item.ChatId, item);
    }

    public async Task<CodeDbModel> ReadItemAsync(int chatId)
    {
       var result = await _client.GetAsync(_dbName + chatId);
       CodeDbModel data = result.ResultAs<CodeDbModel>();
       return data;
    }

    public async Task UpdateItemAsync(CodeDbModel item)
    {
       await _client.UpdateAsync(_dbName + item.ChatId, item);
    }

    public async Task DeleteItemAsync(int chatId)
    {
       await _client.DeleteAsync(_dbName + chatId);
    }
}
