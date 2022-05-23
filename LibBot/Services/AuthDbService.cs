using LibBot.Models.SharePointResponses;
using Microsoft.Extensions.Options;
using LibBot.Models.Configurations;
using System.Net.Http;
using System.Net.Http.Json;
using LibBot.Models.SharePointRequests;
using System.Threading.Tasks;
using System;
using LibBot.Models.DbRequest;
using Newtonsoft.Json;
using LibBot.Models.DbResponse;
using LibBot.Services.Interfaces;

namespace LibBot.Services;

public class AuthDbService : Tokens, IAuthDbService
{
    private IOptions<AuthDbConfiguration> _dbConfiguration;
    private readonly IHttpClientFactory _httpClientFactory;
    public AuthDbService(IOptions<AuthDbConfiguration> dbConfiguration, IHttpClientFactory httpClientFactory)
    {
        _dbConfiguration = dbConfiguration;
        _httpClientFactory = httpClientFactory;
    }

    private async Task GetTokens(object requestData, bool refresh)
    {
        var client = _httpClientFactory.CreateClient("AuthDb");
        var content = JsonContent.Create(requestData);
        var uri =  refresh ? _dbConfiguration.Value.RefreshAddress : client.BaseAddress.ToString();
        var responce = await client.PostAsync(uri, content);
        var stringResponce = await responce.Content.ReadAsStringAsync();
        var data = JsonConvert.DeserializeObject<DbResponse>(stringResponce);
        SetDataTokens(data);
    }

    public async Task<string> GetAccessToken()
    {
        if(Token is null)
        {
            var requestData = new AuthDbRequest() { Email = _dbConfiguration.Value.Login, Password = _dbConfiguration.Value.Password, ReturnSecureToken = true};
            await GetTokens(requestData, false);
            return Token;
        }

        if(DateTime.Now <= CreateTokenDate.AddSeconds(Convert.ToInt32(ExpiresIn)).AddMinutes(-1))
        {
            return Token;
        }
        else
        {
            var requestData = new AuthDbRefreshRequest() { RefreshToken = RefreshToken };
            await GetTokens(requestData, true);
            return Token;
        }
    }
}
