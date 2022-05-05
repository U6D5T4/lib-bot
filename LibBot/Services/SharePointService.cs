using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using LibBot.Models;
using LibBot.Models.SharePointRequests;
using LibBot.Models.SharePointResponses;
using LibBot.Services.Interfaces;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace LibBot.Services;

public class SharePointService : ISharePointService
{
    private readonly IHttpClientFactory _clientFactory;
    public static int AmountBooks { get; } = 8;
    public List<string> BookPaths { get; }

    public SharePointService(IHttpClientFactory clientFactory, IOptions<BookPaths> bookPaths)
    {
        _clientFactory = clientFactory;
        BookPaths = new List<string>(bookPaths.Value.Paths);
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

    public async Task<UserDataResponse> GetUserDataFromSharePointAsync(string login)
    {
        var client = _clientFactory.CreateClient("SharePoint");
        var httpResponse = await client.GetAsync($"_api/web/siteusers?$filter=Email eq '{login}'&$select=Email,Id");
        var contentsString = await httpResponse.Content.ReadAsStringAsync();
        var userDataString = string.Join("", contentsString.SkipWhile(ch => ch != '[').Skip(1).TakeWhile(ch => ch != ']'));
        var userData = JsonConvert.DeserializeObject<UserDataResponse>(userDataString);
        return userData;
    }

    public async Task<List<BookDataResponse>> GetBooksFromSharePointAsync(int pageNumber)
    {
        var client = _clientFactory.CreateClient("SharePoint");

        var httpResponse = await client.GetAsync($"_api/web/lists/GetByTitle('Books')/items?$select=Title,Id,BookReaderId&$skiptoken=Paged=TRUE%26p_ID={pageNumber * AmountBooks}&$top={AmountBooks}");

        var contentsString = await httpResponse.Content.ReadAsStringAsync();
        var dataBooks = Book.FromJson(contentsString);

        var result = Book.GetBookDataResponse(dataBooks);

        if (result.Count == 0)
            result = new List<BookDataResponse>();

        return result;
    }



    public async Task<List<BookDataResponse>> GetBooksFromSharePointAsync(int pageNumber, int userId)
    {
        var client = _clientFactory.CreateClient("SharePoint");

        var httpResponse = await client.GetAsync($"_api/web/lists/GetByTitle('Books')/items?$select=Title,Id,TakenToRead&$skiptoken=Paged=TRUE%26p_ID={pageNumber * AmountBooks}&$top={AmountBooks}&$filter=BookReaderId eq {userId}");


        var contentsString = await httpResponse.Content.ReadAsStringAsync();
        var dataBooks = Book.FromJson(contentsString);

        var result = Book.GetBookDataResponse(dataBooks);

        if (result.Count == 0)
            result = new List<BookDataResponse>();

        return result;
    }

    public async Task<List<BookDataResponse>> GetBooksFromSharePointAsync(int pageNumber, string searchQuery)
    {
        var client = _clientFactory.CreateClient("SharePoint");

        var httpResponse = await client.GetAsync($"_api/web/lists/GetByTitle('Books')/items?$select=Title,Id,BookReaderId&$skiptoken=Paged=TRUE%26p_ID={pageNumber * AmountBooks}&$top={AmountBooks}&$filter=substringof('{searchQuery}', Title)");
        var contentsString = await httpResponse.Content.ReadAsStringAsync();
        var dataBooks = Book.FromJson(contentsString);

        var result = Book.GetBookDataResponse(dataBooks);

        if (result.Count == 0)
            result = new List<BookDataResponse>();

        return result;
    }

    public async Task<List<BookDataResponse>> GetBooksFromSharePointAsync(int pageNumber, List<string> filters)
    {
        var client = _clientFactory.CreateClient("SharePoint");

        var httpResponse = await client.GetAsync($"_api/web/lists/GetByTitle('Books')/items?$select=Title,Id,BookReaderId,Technology&$top=300");
        var contentsString = await httpResponse.Content.ReadAsStringAsync();
        var dataBooks = Book.FromJson(contentsString);

        var result = Book.GetBookDataResponse(dataBooks);

        if (result.Count == 0)
            result = new List<BookDataResponse>();

        var filteredBooks = filters is null ? result : result.Where(book => filters.Any(filter => book.Technology.Results.Any(tech => tech.Label == filter)));
        return filteredBooks.Skip(pageNumber * AmountBooks).Take(AmountBooks).ToList();
    }

    private async Task<string> GetFormDigestValueFromSharePointAsync()
    {
        var client = _clientFactory.CreateClient("SharePoint");
        var emptyJson = new { };
        string json = JsonConvert.SerializeObject(emptyJson);
        StringContent httpContent = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        var httpResponse = await client.PostAsync("https://u6.itechart-group.com:8443/_api/contextinfo", httpContent);
        var contentsString = await httpResponse.Content.ReadAsStringAsync();
        var dataformDigestValue = Book.FromJson(contentsString);
        var formDigestValue = Book.GetFormDigestValue(dataformDigestValue);
        return formDigestValue;
    }

    public async Task<bool> ChangeBookStatus(long chatId, int bookId, ChangeBookStatusRequest bookBorrowRequest)
    {
        var client = _clientFactory.CreateClient("SharePoint");
        var formDigestValue = await GetFormDigestValueFromSharePointAsync();

        string json = JsonConvert.SerializeObject(bookBorrowRequest);
        StringContent httpContent = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        client.DefaultRequestHeaders.Add("If-Match", "*");
        client.DefaultRequestHeaders.Add("X-HTTP-Method", "MERGE");
        client.DefaultRequestHeaders.Add("X-RequestDigest", formDigestValue);

        var httpResponse = await client.PostAsync($"_api/web/lists/GetByTitle('Books')/items({bookId})", httpContent);
        return httpResponse.IsSuccessStatusCode;
    }
}