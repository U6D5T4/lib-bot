using FireSharp.Interfaces;
using LibBot.Models;
using LibBot.Services.Interfaces;
using Newtonsoft.Json;
using System.Collections.Generic;
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
        await _client.SetAsync(_dbName + item.ChatId + '/' + item.MessageId, item);
    }

    public async Task<ChatDbModel> ReadItemAsync(long chatId, long inlineMessageId)
    {
        var result = await _client.GetAsync(_dbName + chatId + '/' + inlineMessageId);
        var data = result.ResultAs<ChatDbModel>();
        return data;
    }

    public async Task UpdateItemAsync(ChatDbModel item) 
    {
        await _client.UpdateAsync(_dbName + item.ChatId + '/' + item.MessageId, item);
    }

    public async Task DeleteItemAsync(long chatId, long inlineMessageId)
    {
        await _client.DeleteAsync(_dbName + chatId + '/' + inlineMessageId);
    }

    public async Task<IEnumerable<ChatDbModel>> ReadUserItemsAsync(long chatId)
    {
        var result = await _client.GetAsync($"{_dbName}/{chatId}");
        var data = JsonConvert.DeserializeObject<Dictionary<string, ChatDbModel>>(result.Body.ToString());

        List<ChatDbModel> users = new List<ChatDbModel>();
        foreach (var key in data.Keys)
        {
            var value = data[key];
            users.Add(value);
        }

        return users;
    }
}
