using FireSharp.Interfaces;
using LibBot.Models;
using LibBot.Services.Interfaces;
using System.Threading.Tasks;
using System.Resources;
using System.Reflection;

namespace LibBot.Services;

public class CodeDbService:ICodeDbService
{
    private readonly IConfigureDb _configureDb;
    private readonly IFirebaseClient _client;
    private ResourceManager _resourceReader;
    public CodeDbService(IConfigureDb configureDb)
    {
        _configureDb = configureDb;
        _client = _configureDb.GetFirebaseClient();
        _resourceReader = new ResourceManager("LibBot.Resources.Resource", Assembly.GetExecutingAssembly());
    }

    public async Task CreateItemAsync(CodeDbModel item)
    {
       await _client.SetAsync(_resourceReader.GetString("Code_DbName") + '/' + item.ChatId, item);
    }

    public async Task<CodeDbModel> ReadItemAsync(long chatId) 
    {
       var result = await _client.GetAsync(_resourceReader.GetString("Code_DbName") + '/' + chatId);
       CodeDbModel data = result.ResultAs<CodeDbModel>();
       return data;
    }

    public async Task UpdateItemAsync(CodeDbModel item)
    {
       await _client.UpdateAsync(_resourceReader.GetString("Code_DbName") + '/' + item.ChatId, item);
    }

    public async Task DeleteItemAsync(long chatId)
    {
       await _client.DeleteAsync(_resourceReader.GetString("Code_DbName") + '/' + chatId);
    }
}
