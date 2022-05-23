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
    public AuthDbService(IOptions<AuthDbConfiguration> dbConfiguration)
    {
        _dbConfiguration = dbConfiguration;
    }

    public async Task GetTokens()
    {
        using var client = new HttpClient();
        var requestData = new AuthDbRequest() { Email = _dbConfiguration.Value.Login, Password = _dbConfiguration.Value.Password, ReturnSecureToken = true};
        var content = JsonContent.Create(requestData);
        var responce = await client.PostAsync(_dbConfiguration.Value.BaseAddress, content);
        var stringResponce = await responce.Content.ReadAsStringAsync();
        var data = JsonConvert.DeserializeObject<AuthDbResponse>(stringResponce);
        Token = data.IdToken;
        RefreshToken = data.RefreshToken;
        ExpiresIn = data.ExpiresIn;
        CreateTokenDate = DateTime.Now;
    }

    public async Task UpdateTokens()
    {
        using var client = new HttpClient();
        var requestParameter = new AuthDbRefreshRequest() { RefreshToken = RefreshToken};
        var content = JsonContent.Create(requestParameter);
        var responce = await client.PostAsync(_dbConfiguration.Value.RefreshAddress, content);
        var stringResponce = await responce.Content.ReadAsStringAsync();
        var data = JsonConvert.DeserializeObject<AuthRefreshDbResponce>(stringResponce);
        Token = data.Id_Token;
        RefreshToken = data.Refresh_Token;
        ExpiresIn= data.Expires_In;
        CreateTokenDate= DateTime.Now;
    }

    public async Task<string> GetAccessToken()
    {
        if(Token is null)
        {
            await GetTokens();
            return Token;
        }

        if(DateTime.Now <= CreateTokenDate.AddSeconds(Convert.ToInt32(ExpiresIn)))
        {
            return Token;
        }
        else
        {
           await UpdateTokens();
           return Token;
        }
    }
}
