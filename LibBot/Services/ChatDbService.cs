using FireSharp.Interfaces;
using LibBot.Models;
using LibBot.Models.Configurations;
using LibBot.Services.Interfaces;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Resources;
using System.Threading.Tasks;

namespace LibBot.Services;

public class ChatDbService: IChatDbService
{
    private readonly IConfigureDb _configureDb;
    private readonly IFirebaseClient _client;
    private ResourceManager _resourceReader;
    private IOptions<DbConfiguration> _dbConfiguration;
    private readonly IAuthDbService _authDbService;
    public ChatDbService(IConfigureDb configureDb, IAuthDbService authDbService, IOptions<DbConfiguration> dbConfiguration)
    {
        _configureDb = configureDb;
        _client = _configureDb.GetFirebaseClient();
        _resourceReader = new ResourceManager("LibBot.Resources.Resource", Assembly.GetExecutingAssembly());
        _dbConfiguration = dbConfiguration;
        _authDbService = authDbService;
    }

    public async Task CreateItemAsync(ChatDbModel item)
    {
        var token = await _authDbService.GetAccessToken();
        using var client = new HttpClient();
        var uri = _dbConfiguration.Value.BasePath + _resourceReader.GetString("Chat_DbName") + '/' + item.ChatId + ".json" + $"?auth={token}";
        var res = await client.PutAsync(uri, JsonContent.Create(item));
    }

    public async Task<ChatDbModel> ReadItemAsync(long chatId)
    {
        var token = await _authDbService.GetAccessToken();
        using var client = new HttpClient();
        var uri = _dbConfiguration.Value.BasePath + _resourceReader.GetString("Chat_DbName") + '/' + chatId + ".json" + $"?auth={token}";
        var responce = await client.GetAsync(uri);
        var stringResponce = await responce.Content.ReadAsStringAsync();
        var data = JsonConvert.DeserializeObject<ChatDbModel>(stringResponce);
        return data;
    }

    public async Task UpdateItemAsync(ChatDbModel item) 
    {
        var token = await _authDbService.GetAccessToken();
        using var client = new HttpClient();
        var uri = _dbConfiguration.Value.BasePath + _resourceReader.GetString("Chat_DbName") + '/' + item.ChatId + ".json" + $"?auth={token}";
        await client.PatchAsync(uri, JsonContent.Create(item));
    }

    public async Task DeleteItemAsync(long chatId)
    {
        var token = await _authDbService.GetAccessToken();
        using var client = new HttpClient();
        var uri = _dbConfiguration.Value.BasePath + _resourceReader.GetString("Chat_DbName") + '/' + chatId + ".json" + $"?auth={token}";
        await client.DeleteAsync(uri);
    }
}
