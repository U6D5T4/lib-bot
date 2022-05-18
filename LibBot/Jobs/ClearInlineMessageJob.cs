using FireSharp.Interfaces;
using LibBot.Models;
using LibBot.Models.Configurations;
using LibBot.Services;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Quartz;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Telegram.Bot;

namespace LibBot.Jobs;

class ClearInlineMessageJob : IJob
{
    private readonly ITelegramBotClient _botClient;
    private readonly IFirebaseClient _dbClient;
    private readonly IHttpClientFactory _httpClientFactory;

    public ClearInlineMessageJob(ITelegramBotClient botClient, IHttpClientFactory httpClientFactory, IOptions<DbConfiguration> dbConfiguration)
    {
        _botClient = botClient;
        _httpClientFactory = httpClientFactory;
        _dbClient = new ConfigureDb(dbConfiguration).GetFirebaseClient();
    }

    public async Task<Task> Execute(IJobExecutionContext context)
    {
        var chats = await ReadAllChatsAsync();

        foreach (var chat in chats)
        {
            foreach(var message in chat.CurrentMessagesId)
                await _botClient.DeleteMessageAsync(chat.ChatId, message);
        }

        await _dbClient.DeleteAsync("Chats");

        return Task.CompletedTask;
    }

    async Task IJob.Execute(IJobExecutionContext context)
    {
        await Execute(context);
    }

    private async Task<List<ChatDbModel>> ReadAllChatsAsync()
    {
        var result = await _dbClient.GetAsync("Chats");
        var data = JsonConvert.DeserializeObject<Dictionary<string, ChatDbModel>>(result.Body.ToString());

        List<ChatDbModel> chats = new List<ChatDbModel>();
        foreach (var key in data.Keys)
        {
            var value = data[key];
            chats.Add(value);
        }

        return chats;
    }
}

