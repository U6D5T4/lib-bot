using LibBot.Models.SharePointResponses;
using Microsoft.Extensions.Options;
using LibBot.Models.Configurations;
using System.Net.Http;
using System.Net.Http.Json;
using LibBot.Models.SharePointRequests;
using System.Threading.Tasks;
using System;
using LibBot.Models.DbRequest;

namespace LibBot.Services;

public class AuthDbService
{
    private IOptions<AuthDbConfiguration> _dbConfiguration;
    private static string Token { get; set; }
    private static string RefreshToken { get; set; }
    private static DateTime ExpiredDate { get; set; }

    private static DateTime CreateTokenDate {get;set;}

    public AuthDbService(IOptions<AuthDbConfiguration> dbConfiguration)
    {
        _dbConfiguration = dbConfiguration;
    }
   public async Task GetTokens()
    {
        using var client = new HttpClient();
        var res = new AuthDbRequest() { Login = _dbConfiguration.Value.Login, Password = _dbConfiguration.Value.Password, Parameter = true};
        var content = JsonContent.Create(res);
        var responce = await client.PostAsync(_dbConfiguration.Value.BaseAddress, content);
        var result = await responce.Content.ReadAsStringAsync<AuthDbResponse>();
        Token = result.AccessToken;
        RefreshToken = result.RefreshToken;
        ExpiredDate = result.ExpiredDate;
    }


    public async Task UpdateTokens()
    {
        using var client = new HttpClient();
        var requestParameter = new AuthDbRefreshRequest() { RefreshToken = RefreshToken};
        var content = JsonContent.Create(requestParameter);
        var responce = await client.PostAsync(_dbConfiguration.Value.RefreshAddress, content);
        var result = await responce.Content.ReadAsStringAsync<AuthDbResponse>();
        Token = result.AccessToken;
        RefreshToken = result.RefreshToken;
        ExpiredDate = result.ExpiredDate;
    }

    public async Task<string> GetToken()
    {
        if(Token is null)
        {
            await GetTokens();
            return Token;
        }

        if(DateTime.Now >= CreateTokenDate.AddMilliseconds(ExpiredDate.Millisecond))
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
