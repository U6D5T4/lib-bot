using FireSharp.Interfaces;
using LibBot.Models;
using LibBot.Services.Interfaces;
using System.Threading.Tasks;

namespace LibBot.Services;
public class UserDbService : IUserDbService
{
    private readonly IConfigureDb _configureDb;
    private readonly IFirebaseClient _client;
    private const string _dbName = "Users/";

    public UserDbService(IConfigureDb configureDb)
    {
       _configureDb = configureDb;
       _client = _configureDb.GetFirebaseClient();
    }
    
    public async Task CreateItemAsync(UserDbModel user)
    {
       await _client.SetAsync(_dbName + user.ChatId, user);
    }

    public async Task<UserDbModel> ReadItemAsync(long chatId)
    {
        var result =  await _client.GetAsync(_dbName + chatId);
        var data = result.ResultAs<UserDbModel>();
        return data;
    }

    public async Task UpdateItemAsync(UserDbModel user)
    {
        await _client.UpdateAsync(_dbName + user.ChatId, user);
    }

    public async Task DeleteItemAsync(long chatId)
    {
       await _client.DeleteAsync(_dbName + chatId);
    }
}
