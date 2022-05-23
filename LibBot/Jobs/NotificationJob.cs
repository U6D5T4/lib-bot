using LibBot.Models;
using LibBot.Models.Configurations;
using LibBot.Models.SharePointResponses;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Quartz;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Telegram.Bot;
using System.Resources;
using System.Reflection;
using LibBot.Models.DbRequest;
using System.Net.Http.Json;
using LibBot.Models.DbResponse;
using LibBot.Models.SharePointRequests;

namespace LibBot.Jobs;

class NotificationJob : Tokens, IJob
{
    private readonly ITelegramBotClient _botClient;

    private readonly IHttpClientFactory _httpClientFactory;
    private ResourceManager _resourceReader;
    private IOptions<AuthDbConfiguration> _dbConfiguration;
    public NotificationJob(ITelegramBotClient botClient, IHttpClientFactory httpClientFactory, IOptions<AuthDbConfiguration> dbConfiguration)
    {
        _botClient = botClient;
        _httpClientFactory = httpClientFactory;
        _dbConfiguration = dbConfiguration;
        _resourceReader = new ResourceManager("LibBot.Resources.Resource", Assembly.GetExecutingAssembly());
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
                    if (book.TakenToRead.Value.AddMonths(2).AddDays(-14).ToShortDateString() == DateTime.UtcNow.ToShortDateString())
                    {
                        await _botClient.SendTextMessageAsync(user.ChatId, String.Format(_resourceReader.GetString("BooksReturnPeriodFirst"), book.Title));
                    }

                    if (book.TakenToRead.Value.AddMonths(2).AddDays(-7).ToShortDateString() == DateTime.UtcNow.ToShortDateString())
                    {
                        await _botClient.SendTextMessageAsync(user.ChatId, String.Format(_resourceReader.GetString("BooksReturnPeriodSecond"), book.Title));
                    }

                    if (book.TakenToRead.Value.AddMonths(2).AddDays(-3).ToShortDateString() == DateTime.UtcNow.ToShortDateString())
                    {
                        await _botClient.SendTextMessageAsync(user.ChatId, String.Format(_resourceReader.GetString("BooksReturnPeriodThird"), book.Title));
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
        var token = await GetAccessToken();
        var client = _httpClientFactory.CreateClient("AuthDb");
        var uri = client.BaseAddress + _resourceReader.GetString("User_DbName") + ".json" + $"?auth={token}";
        var responce = await client.GetAsync(uri);
        var stringResponce = await responce.Content.ReadAsStringAsync();
        var data = JsonConvert.DeserializeObject<Dictionary<string, UserDbModel>>(stringResponce);

        List<UserDbModel> users = new List<UserDbModel>();
        foreach (var key in data.Keys)
        {
            var value = data[key];
            users.Add(value);
        }
        return users;
    }

    private async Task GetTokens(object requestData, bool refresh)
    {
        var client = _httpClientFactory.CreateClient("AuthDb");
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