using FireSharp.Interfaces;
using LibBot.Models;
using LibBot.Services.Interfaces;
using System.Threading.Tasks;

namespace LibBot.Services;

public class ChatDbService: IChatDbService
{
    private readonly IConfigureDb _configureDb;
    private readonly IFirebaseClient _client;
    private const string _dbName = "Chats/";

    public ChatDbService(IConfigureDb configureDb)
    {
        _configureDb = configureDb;
        _client = _configureDb.GetFirebaseClient();
    }

    public async Task CreateItemAsync(ChatDbModel item)
    {
        await _client.SetAsync(_dbName + item.ChatId, item);
    }

    public async Task<ChatDbModel> ReadItemAsync(long chatId)
    {
        var result = await _client.GetAsync(_dbName + chatId);
        var data = result.ResultAs<ChatDbModel>();
        return data;
    }

    public async Task UpdateItemAsync(ChatDbModel item) 
    {
        await _client.UpdateAsync(_dbName + item.ChatId, item);
    }

    public async Task DeleteItemAsync(long chatId)
    {
        await _client.DeleteAsync(_dbName + chatId);
    }
}
