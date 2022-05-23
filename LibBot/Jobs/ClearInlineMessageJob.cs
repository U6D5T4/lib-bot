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
        var client = _httpClientFactory.CreateClient("Db");
        var uri = client.BaseAddress + _resourceReader.GetString("Chat_DbName") + ".json" + $"?auth={token}";
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
        var client = _httpClientFactory.CreateClient("Db");
        var uri = client.BaseAddress + _resourceReader.GetString("Chat_DbName") + ".json" + $"?auth={token}";
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

    private async Task GetTokens(object requestData, bool refresh)
    {
         var client = _httpClientFactory.CreateClient("Db");
        var content = JsonContent.Create(requestData);
        var uri = refresh ? _dbConfiguration.Value.RefreshAddress : client.BaseAddress.ToString();
        var responce = await client.PostAsync(uri, content);
        var stringResponce = await responce.Content.ReadAsStringAsync();
        var data = JsonConvert.DeserializeObject<DbResponse>(stringResponce);
        SetDataTokens(data);
    }

    private async Task<string> GetAccessToken()
    {
        if (Token is null)
        {
            var requestData = new AuthDbRequest() { Email = _dbConfiguration.Value.Login, Password = _dbConfiguration.Value.Password, ReturnSecureToken = true };
            await GetTokens(requestData, false);
            return Token;
        }

        if (DateTime.Now <= CreateTokenDate.AddSeconds(Convert.ToInt32(ExpiresIn)).AddMinutes(-1))
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

