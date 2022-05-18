using FireSharp.Interfaces;
using LibBot.Models;
using LibBot.Services.Interfaces;
using System.Threading.Tasks;
using System.Resources;
using System.Reflection;

namespace LibBot.Services;

public class FeedbackDbService : IFeedbackDbService
{
    private readonly IConfigureDb _configureDb;
    private readonly IFirebaseClient _client;
    private ResourceManager _resourceReader;

    public FeedbackDbService(IConfigureDb configureDb)
    {
        _configureDb = configureDb;
        _client = _configureDb.GetFirebaseClient();
        _resourceReader = new ResourceManager("LibBot.Resources.Resource", Assembly.GetExecutingAssembly());
    }

    public async Task CreateItemAsync(UserFeedbackDbModel item)
    {
        await _client.PushAsync(_resourceReader.GetString("Feedback_DbName") + '/', item);
    }
}
