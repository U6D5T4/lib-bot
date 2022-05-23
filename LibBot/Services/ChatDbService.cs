using LibBot.Models;
using LibBot.Services.Interfaces;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Resources;
using System.Threading.Tasks;

namespace LibBot.Services;

public class ChatDbService: IChatDbService
{
    private ResourceManager _resourceReader;
    private readonly IAuthDbService _authDbService;
    private readonly IHttpClientFactory _httpClientFactory;
    public ChatDbService(IAuthDbService authDbService, IHttpClientFactory httpClientFactory)
    {
        _resourceReader = new ResourceManager("LibBot.Resources.Resource", Assembly.GetExecutingAssembly());
        _authDbService = authDbService;
        _httpClientFactory = httpClientFactory;
    }

    public async Task CreateItemAsync(ChatDbModel item)
    {
        var token = await _authDbService.GetAccessToken();
        var client = _httpClientFactory.CreateClient("Db");
        var uri = client.BaseAddress + _resourceReader.GetString("Chat_DbName") + '/' + item.ChatId + ".json" + $"?auth={token}";
        var res = await client.PutAsync(uri, JsonContent.Create(item));
    }

    public async Task<ChatDbModel> ReadItemAsync(long chatId)
    {
        var token = await _authDbService.GetAccessToken();
        var client = _httpClientFactory.CreateClient("Db");
        var uri = client.BaseAddress + _resourceReader.GetString("Chat_DbName") + '/' + chatId + ".json" + $"?auth={token}";
        var responce = await client.GetAsync(uri);
        var stringResponce = await responce.Content.ReadAsStringAsync();
        var data = JsonConvert.DeserializeObject<ChatDbModel>(stringResponce);
        return data;
    }

    public async Task UpdateItemAsync(ChatDbModel item) 
    {
        var token = await _authDbService.GetAccessToken();
        var client = _httpClientFactory.CreateClient("Db");
        var uri = client.BaseAddress + _resourceReader.GetString("Chat_DbName") + '/' + item.ChatId + ".json" + $"?auth={token}";
        await client.PatchAsync(uri, JsonContent.Create(item));
    }

    public async Task DeleteItemAsync(long chatId)
    {
        var token = await _authDbService.GetAccessToken();
        var client = _httpClientFactory.CreateClient("Db");
        var uri = client.BaseAddress + _resourceReader.GetString("Chat_DbName") + '/' + chatId + ".json" + $"?auth={token}";
        await client.DeleteAsync(uri);
    }
}
