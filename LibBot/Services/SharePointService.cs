using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using LibBot.Models.SharePointResponses;
using LibBot.Services.Interfaces;
using Newtonsoft.Json;

namespace LibBot.Services;

public class SharePointService : ISharePointService
{
    private readonly IHttpClientFactory _clientFactory;

    public SharePointService(IHttpClientFactory clientFactory)
    {
        _clientFactory = clientFactory;
    }

    public async Task<bool> IsUserExistInSharePointAsync(string login)
    {
        var client = _clientFactory.CreateClient("SharePoint");

        var httpResponse = await client.GetAsync($"_api/web/siteusers?$filter=Email eq '{login}'&$select=Email");
        if (!httpResponse.IsSuccessStatusCode)
        {
            return false;
        }

        var contentsString = await httpResponse.Content.ReadAsStringAsync();
        var userDataString = string.Join("", contentsString.SkipWhile(ch => ch != '[').Skip(1).TakeWhile(ch => ch != ']'));
        var userData = JsonConvert.DeserializeObject<UserDataResponse>(userDataString);
         
        return userData?.Email != null && userData.Email.Contains(login, StringComparison.InvariantCultureIgnoreCase);
    }
}