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
    private ResourceManager _resourceReader;
    private readonly IAuthDbService _authDbService;
    private IOptions<DbConfiguration> _dbConfiguration;
    private readonly IHttpClientFactory _httpClientFactory;

    public FeedbackDbService(IAuthDbService authDbService, IHttpClientFactory httpClientFactory, IOptions<DbConfiguration> dbConfiguration)
    {
        _resourceReader = new ResourceManager("LibBot.Resources.Resource", Assembly.GetExecutingAssembly());
        _dbConfiguration = dbConfiguration;
        _authDbService = authDbService;
        _httpClientFactory = httpClientFactory;
    }

    public async Task CreateItemAsync(UserFeedbackDbModel item)
    {
        var token = await _authDbService.GetAccessToken();
        var client = _httpClientFactory.CreateClient("Firebase");
        var uri = client.BaseAddress + _resourceReader.GetString("Feedback_DbName") + '/' + item.ChatId + ".json" + $"?auth={token}";
        var res = await client.PostAsync(uri, JsonContent.Create(item));
    }
}
