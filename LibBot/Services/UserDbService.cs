using FireSharp.Interfaces;
using LibBot.Models;
using LibBot.Services.Interfaces;
using System.Threading.Tasks;
using System.Resources;
using System.Reflection;
using LibBot.Models.Configurations;
using Microsoft.Extensions.Options;
using System.Net.Http;
using Newtonsoft.Json;
using System.Net.Http.Json;

namespace LibBot.Services;
public class UserDbService : IUserDbService
{
    private readonly IConfigureDb _configureDb;
    private readonly IFirebaseClient _client;
    private ResourceManager _resourceReader;
    private readonly IAuthDbService _authDbService;
    private IOptions<DbConfiguration> _dbConfiguration;

    public UserDbService(IConfigureDb configureDb, IAuthDbService authDbService, IOptions<DbConfiguration> dbConfiguration)
    {
        _configureDb = configureDb;
        _client = _configureDb.GetFirebaseClient();
        _resourceReader = new ResourceManager("LibBot.Resources.Resource", Assembly.GetExecutingAssembly());
        _dbConfiguration = dbConfiguration;
        _authDbService = authDbService;
    }
    
    public async Task CreateItemAsync(UserDbModel item)
    {
        var token = await _authDbService.GetAccessToken();
        using var client = new HttpClient();
        var uri = _dbConfiguration.Value.BasePath + _resourceReader.GetString("User_DbName") + '/' + item.ChatId + ".json" + $"?auth={token}";
        var res = await client.PutAsync(uri, JsonContent.Create(item));
    }

    public async Task<UserDbModel> ReadItemAsync(long chatId)
    {
        var token = await _authDbService.GetAccessToken();
        using var client = new HttpClient();
        var uri = _dbConfiguration.Value.BasePath + _resourceReader.GetString("User_DbName") + '/' + chatId + ".json" + $"?auth={token}";
        var responce = await client.GetAsync(uri);
        var stringResponce = await responce.Content.ReadAsStringAsync();
        var data = JsonConvert.DeserializeObject<UserDbModel>(stringResponce);
        return data;
    }

    public async Task UpdateItemAsync(UserDbModel item)
    {
        var token = await _authDbService.GetAccessToken();
        using var client = new HttpClient();
        var uri = _dbConfiguration.Value.BasePath + _resourceReader.GetString("User_DbName") + '/' + item.ChatId + ".json" + $"?auth={token}";
        await client.PatchAsync(uri, JsonContent.Create(item));
    }

    public async Task DeleteItemAsync(long chatId)
    {
        var token = await _authDbService.GetAccessToken();
        using var client = new HttpClient();
        var uri = _dbConfiguration.Value.BasePath + _resourceReader.GetString("User_DbName") + '/' + chatId + ".json" + $"?auth={token}";
        await client.DeleteAsync(uri);
    }
}
