using FireSharp.Interfaces;
using LibBot.Models;
using LibBot.Models.Configurations;
using LibBot.Models.SharePointResponses;
using LibBot.Services;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Quartz;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Telegram.Bot;

namespace LibBot.Jobs;

class NotificationJob : IJob
{
    private readonly ITelegramBotClient _botClient;
    private readonly IFirebaseClient _dbClient;


    private readonly IHttpClientFactory _httpClientFactory;
    public NotificationJob(ITelegramBotClient botClient, IHttpClientFactory httpClientFactory, IOptions<DbConfiguration> dbConfiguration)
    {
        _botClient = botClient;
        _httpClientFactory = httpClientFactory;
        _dbClient = new ConfigureDb(dbConfiguration).GetFirebaseClient();
    }
    public async Task<Task> Execute(IJobExecutionContext context)
    {

        var books = await GetBooksFromSharePointForReminderAsync();
        var users = await ReadAllUsersAsync();

        foreach (var book in books)
        {
            foreach (var user in users)
            {
                if (book.BookReaderId == user.SharePointId)
                {
                    var date = (DateTime)book.TakenToRead;

                    if (date.AddMonths(2).AddDays(-14).ToShortDateString() == DateTime.UtcNow.ToShortDateString())
                    {
                        await _botClient.SendTextMessageAsync(user.ChatId, "There are 2 weeks left until the end of the book return period");
                    }

                    if (date.AddMonths(2).AddDays(-7).ToShortDateString() == DateTime.UtcNow.ToShortDateString())
                    {
                        await _botClient.SendTextMessageAsync(user.ChatId, "There are 1 week left until the end of the book return period");
                    }

                    if (date.AddMonths(2).AddDays(-3).ToShortDateString() == DateTime.UtcNow.ToShortDateString())
                    {
                        await _botClient.SendTextMessageAsync(user.ChatId, "There are 3 days left until the end of the book return period");
                    }

                    break;
                }
            }
        }

        return Task.CompletedTask;
    }

    async Task IJob.Execute(IJobExecutionContext context)
    {
        await Execute(context);
    }

    private async Task<List<BookDataResponse>> GetBooksFromSharePointForReminderAsync()
    {
        var client = _httpClientFactory.CreateClient("SharePoint");

        var httpResponse = await client.GetAsync($"_api/web/lists/GetByTitle('Books')/items?$select=Id,BookReaderId,TakenToRead&$filter=TakenToRead ne null");
        var contentsString = await httpResponse.Content.ReadAsStringAsync();
        var dataBooks = Book.FromJson(contentsString);
        return Book.GetBookDataResponse(dataBooks);
    }

    private async Task<List<UserDbModel>> ReadAllUsersAsync()
    {
        var result = await _dbClient.GetAsync("Users");
        var data = JsonConvert.DeserializeObject<Dictionary<string, UserDbModel>>(result.Body.ToString());

        List<UserDbModel> users = new List<UserDbModel>();
        foreach (var key in data.Keys)
        {
            var value = data[key];
            users.Add(value);
        }
        return users;
    }
}
