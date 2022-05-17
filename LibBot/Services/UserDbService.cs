using FireSharp.Interfaces;
using LibBot.Models;
using LibBot.Services.Interfaces;
using System.Threading.Tasks;
using System.Resources;
using System.Reflection;

namespace LibBot.Services;
public class UserDbService : IUserDbService
{
    private readonly IConfigureDb _configureDb;
    private readonly IFirebaseClient _client;
    private ResourceManager _resourceReader;
    
    public UserDbService(IConfigureDb configureDb)
    {
       _configureDb = configureDb;
       _client = _configureDb.GetFirebaseClient();
       _resourceReader = new ResourceManager("LibBot.Resources.Resource", Assembly.GetExecutingAssembly());
    }
    
    public async Task CreateItemAsync(UserDbModel user)
    {
       await _client.SetAsync(_resourceReader.GetString("User_DbName") + '/' + user.ChatId, user);
    }

    public async Task<UserDbModel> ReadItemAsync(long chatId)
    {
        var result =  await _client.GetAsync(_resourceReader.GetString("User_DbName") + '/' + chatId);
        var data = result.ResultAs<UserDbModel>();
        return data;
    }

    public async Task UpdateItemAsync(UserDbModel user)
    {
        await _client.UpdateAsync(_resourceReader.GetString("User_DbName") + '/' + user.ChatId, user);
    }

    public async Task DeleteItemAsync(long chatId)
    {
       await _client.DeleteAsync(_resourceReader.GetString("User_DbName") + '/' + chatId);
    }
}
