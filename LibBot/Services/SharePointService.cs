using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using LibBot.Models;
using LibBot.Models.SharePointRequests;
using LibBot.Models.SharePointResponses;
using LibBot.Services.Interfaces;
using Newtonsoft.Json;

namespace LibBot.Services;

public class SharePointService : ISharePointService
{
    private readonly IHttpClientFactory _clientFactory;

    public static int AmountBooks { get; set; } = 8;
    public static int PageNumber { get; set; } = 0;

    private const int PageSize = 8;
    public static int BorrowBookId { get; set; }


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

    public async Task<UserDataResponse> GetUserDataFromSharePointAsync(string login)
    {
        var client = _clientFactory.CreateClient("SharePoint");
        var httpResponse = await client.GetAsync($"_api/web/siteusers?$filter=Email eq '{login}'&$select=Email,Id");
        var contentsString = await httpResponse.Content.ReadAsStringAsync();
        var userDataString = string.Join("", contentsString.SkipWhile(ch => ch != '[').Skip(1).TakeWhile(ch => ch != ']'));
        var userData = JsonConvert.DeserializeObject<UserDataResponse>(userDataString);
        return userData;

    }

    public async Task<List<BookDataResponse>> GetBooksFromSharePointAsync()
    {
        var books = new List<BookDataResponse>();

        try
        {
            var client = _clientFactory.CreateClient("SharePoint");

            var httpResponse = await client.GetAsync($"_api/web/lists/GetByTitle('Books')/items?$select=Title,Id&$skiptoken=Paged=TRUE%26p_ID={PageNumber}&$top={AmountBooks}&$filter=BookReaderId eq null");
            if (!httpResponse.IsSuccessStatusCode)
            {
                throw new Exception();
            }

            var contentsString = await httpResponse.Content.ReadAsStringAsync();
            var dataBooks = Book.FromJson(contentsString);
            books = Book.GetBookDataResponse(dataBooks);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

        return books.ToList();
    }

    public async Task<string> GetFormDigestValueFromSharePointAsync()
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

    public async Task<bool> BorrowBook(long chatId, int bookId, UserDbModel userData)
    {
        var client = _clientFactory.CreateClient("SharePoint");
        var formDigestValue = await GetFormDigestValueFromSharePointAsync();

        BookBorrowRequest borrowBook = new BookBorrowRequest();
        borrowBook.EditorId = userData.SharePointId;
        borrowBook.BookReaderId = userData.SharePointId;
        borrowBook.BookReaderStringId = userData.SharePointId.ToString();
        borrowBook.Modified = DateTime.UtcNow;
        borrowBook.TakenToRead = DateTime.UtcNow;

        string json = JsonConvert.SerializeObject(borrowBook);
        StringContent httpContent = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        client.DefaultRequestHeaders.Add("If-Match", "*");
        client.DefaultRequestHeaders.Add("X-HTTP-Method", "MERGE");
        client.DefaultRequestHeaders.Add("X-RequestDigest", formDigestValue);

        var httpResponse = await client.PostAsync($"_api/web/lists/GetByTitle('Books')/items({bookId})", httpContent);
        if (httpResponse.IsSuccessStatusCode)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public  void SetNextPageNumberValue() => PageNumber += 8;
    
    public  void SetPreviousPageNumberValue()
    {
        if (PageNumber - PageSize > 0)
            PageNumber -= PageSize;
        else
            PageNumber = 0;
    }
    
    public void  SetDefaultPageNumberValue() => PageNumber = 0;

}