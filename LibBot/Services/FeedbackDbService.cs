using FireSharp.Interfaces;
using LibBot.Models;
using LibBot.Services.Interfaces;
using System.Threading.Tasks;
using System.Resources;
using System.Reflection;
using Microsoft.Extensions.Options;
using LibBot.Models.Configurations;
using System.Net.Http.Json;
using System.Net.Http;

namespace LibBot.Services;

public class FeedbackDbService : IFeedbackDbService
{
    private readonly IConfigureDb _configureDb;
    private readonly IFirebaseClient _client;
    private ResourceManager _resourceReader;
    private readonly IAuthDbService _authDbService;
    private IOptions<DbConfiguration> _dbConfiguration;

    public FeedbackDbService(IConfigureDb configureDb, IAuthDbService authDbService, IOptions<DbConfiguration> dbConfiguration)
    {
        _configureDb = configureDb;
        _client = _configureDb.GetFirebaseClient();
        _resourceReader = new ResourceManager("LibBot.Resources.Resource", Assembly.GetExecutingAssembly());
        _dbConfiguration = dbConfiguration;
        _authDbService = authDbService;
    }

    public async Task CreateItemAsync(UserFeedbackDbModel item)
    {
        var token = await _authDbService.GetAccessToken();
        using var client = new HttpClient();
        var uri = _dbConfiguration.Value.BasePath + _resourceReader.GetString("Feedback_DbName") + '/' + item.ChatId + ".json" + $"?auth={token}";
        var res = await client.PostAsync(uri, JsonContent.Create(item));
    }
}
