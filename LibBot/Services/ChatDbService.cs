using FireSharp.Interfaces;
using LibBot.Models;
using LibBot.Services.Interfaces;
using System.Reflection;
using System.Resources;
using System.Threading.Tasks;

namespace LibBot.Services;

public class ChatDbService: IChatDbService
{
    private readonly IConfigureDb _configureDb;
    private readonly IFirebaseClient _client;
    private ResourceManager _resourceReader;

    public ChatDbService(IConfigureDb configureDb)
    {
        _configureDb = configureDb;
        _client = _configureDb.GetFirebaseClient();
        _resourceReader = new ResourceManager("LibBot.Resources.Resource", Assembly.GetExecutingAssembly());
    }

    public async Task CreateItemAsync(ChatDbModel item)
    {
        await _client.SetAsync(_resourceReader.GetString("Chat_DbName") + '/' + item.ChatId, item);
    }

    public async Task<ChatDbModel> ReadItemAsync(long chatId)
    {
        var result = await _client.GetAsync(_resourceReader.GetString("Chat_DbName") + '/' + chatId);
        var data = result.ResultAs<ChatDbModel>();
        return data;
    }

    public async Task UpdateItemAsync(ChatDbModel item) 
    {
        await _client.UpdateAsync(_resourceReader.GetString("Chat_DbName") + '/' + item.ChatId, item);
    }

    public async Task DeleteItemAsync(long chatId)
    {
        await _client.DeleteAsync(_resourceReader.GetString("Chat_DbName") + '/' + chatId);
    }
}
