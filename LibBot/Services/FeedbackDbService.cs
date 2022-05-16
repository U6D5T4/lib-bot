using FireSharp.Interfaces;
using LibBot.Models;
using LibBot.Services.Interfaces;
using System.Threading.Tasks;

namespace LibBot.Services;

public class FeedbackDbService : IFeedbackDbService
{
    private readonly IConfigureDb _configureDb;
    private readonly IFirebaseClient _client;
    private const string _dbName = "Feedbacks/";

    public FeedbackDbService(IConfigureDb configureDb)
    {
        _configureDb = configureDb;
        _client = _configureDb.GetFirebaseClient();
    }

    public async Task CreateItemAsync(UserFeedbackDbModel item)
    {
        await _client.PushAsync(_dbName, item);
    }
}
