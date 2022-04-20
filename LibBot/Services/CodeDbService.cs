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

    public async Task CreateVerifyCodeAsync(ICodeDbModel code)
    {
       await _client.SetAsync(_dbName + code.ChatId, code);
    }

    public async Task<ICodeDbModel> ReadVerifyCodeAsync(int chatId)
    {
       var result = await _client.GetAsync(_dbName + chatId);
       ICodeDbModel data = result.ResultAs<ICodeDbModel>();
       return data;
    }

    public async Task UpdateVerifyCodeAsync(ICodeDbModel code)
    {
       await _client.UpdateAsync(_dbName + code.ChatId, code);
    }

    public async Task DeleteVerifyCodeAsync(int chatId)
    {
       await _client.DeleteAsync(_dbName + chatId);
    }
}
