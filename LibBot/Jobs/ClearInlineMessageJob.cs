using LibBot.Models;
using LibBot.Models.Configurations;
using LibBot.Models.DbRequest;
using LibBot.Models.DbResponse;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Quartz;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Telegram.Bot;
using System.Resources;
using System.Reflection;
using LibBot.Models.SharePointResponses;
using LibBot.Models.SharePointRequests;

namespace LibBot.Jobs;

class ClearInlineMessageJob : Tokens, IJob
{
    private readonly ITelegramBotClient _botClient;
    private IOptions<AuthDbConfiguration> _dbConfiguration;
    private readonly IHttpClientFactory _httpClientFactory;
    private ResourceManager _resourceReader;

    public ClearInlineMessageJob(ITelegramBotClient botClient, IHttpClientFactory httpClientFactory, IOptions<AuthDbConfiguration> dbConfiguration)
    {
        _botClient = botClient;
        _httpClientFactory = httpClientFactory;
        _dbConfiguration = dbConfiguration;
        _resourceReader = new ResourceManager("LibBot.Resources.Resource", Assembly.GetExecutingAssembly());
    }

    public async Task<Task> Execute(IJobExecutionContext context)
    {
        var chats = await ReadAllChatsAsync();

        foreach (var chat in chats)
        {
            foreach(var message in chat.CurrentMessagesId)
                await _botClient.DeleteMessageAsync(chat.ChatId, message);
        }

        var token = await GetAccessToken();
        using var client = new HttpClient();
        var uri = _dbConfiguration.Value.BaseAddress + _resourceReader.GetString("Chat_DbName") + ".json" + $"?auth={token}";
        await client.DeleteAsync(uri);

        return Task.CompletedTask;
    }

    async Task IJob.Execute(IJobExecutionContext context)
    {
        await Execute(context);
    }

    private async Task<List<ChatDbModel>> ReadAllChatsAsync()
    {
        var token = await GetAccessToken();
        using var client = new HttpClient();
        var uri = _dbConfiguration.Value.BaseAddress + _resourceReader.GetString("Chat_DbName") + ".json" + $"?auth={token}";
        var responce = await client.GetAsync(uri);
        var stringResponce = await responce.Content.ReadAsStringAsync();
        var data = JsonConvert.DeserializeObject<Dictionary<string, ChatDbModel>>(stringResponce);

        List<ChatDbModel> chats = new List<ChatDbModel>();
        foreach (var key in data.Keys)
        {
            var value = data[key];
            chats.Add(value);
        }

        return chats;
    }

    private async Task UpdateTokens()
    {
        using var client = new HttpClient();
        var requestParameter = new AuthDbRefreshRequest() { RefreshToken = RefreshToken };
        var content = JsonContent.Create(requestParameter);
        var responce = await client.PostAsync(_dbConfiguration.Value.RefreshAddress, content);
        var stringResponce = await responce.Content.ReadAsStringAsync();
        var data = JsonConvert.DeserializeObject<AuthRefreshDbResponce>(stringResponce);
        Token = data.Id_Token;
        RefreshToken = data.Refresh_Token;
        ExpiresIn = data.Expires_In;
        CreateTokenDate = DateTime.Now;
    }

    private async Task GetTokens()
    {
        using var client = new HttpClient();
        var requestData = new AuthDbRequest() { Email = _dbConfiguration.Value.Login, Password = _dbConfiguration.Value.Password, ReturnSecureToken = true };
        var content = JsonContent.Create(requestData);
        var responce = await client.PostAsync(_dbConfiguration.Value.BaseAddress, content);
        var stringResponce = await responce.Content.ReadAsStringAsync();
        var data = JsonConvert.DeserializeObject<AuthDbResponse>(stringResponce);
        Token = data.IdToken;
        RefreshToken = data.RefreshToken;
        ExpiresIn = data.ExpiresIn;
        CreateTokenDate = DateTime.Now;
    }

    private async Task<string> GetAccessToken()
    {
        if (Token is null)
        {
            await GetTokens();
            return Token;
        }

        if (DateTime.Now <= CreateTokenDate.AddSeconds(Convert.ToInt32(ExpiresIn)))
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

