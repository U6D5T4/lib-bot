using FireSharp.Interfaces;
using LibBot.Models;
using LibBot.Services.Interfaces;
using System.Threading.Tasks;

namespace LibBot.Services;
public class UserDbService : IUserDbService
{
    private readonly IConfigureDb _configureDb;
    private readonly IFirebaseClient _client;
    private static readonly string _dbName = "Users/";

    public UserDbService(IConfigureDb configureDb)
    {
       _configureDb = configureDb;
       _client = _configureDb.GetFirebaseClient();
    }
    
    public async Task CreateUserAsync(IUserDbModel user)
    {
       await _client.SetAsync(_dbName + user.ChatId, user);
    }

    public async Task<IUserDbModel> ReadUserAsync(int chatId)
    {
        var result =  await _client.GetAsync(_dbName + chatId);
        var data = result.ResultAs<IUserDbModel>();
        return data;
    }

    public async Task UpdateUserAsync(IUserDbModel user)
    {
        await _client.UpdateAsync(_dbName + user.ChatId, user);
    }

    public async Task DeleteUserAsync(int chatId)
    {
       await _client.DeleteAsync(_dbName + chatId);
    }
}
