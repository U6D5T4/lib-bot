using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using LibBot.Models.SharePointRequests;
using LibBot.Models.SharePointResponses;
using LibBot.Services.Interfaces;
using Newtonsoft.Json;
using System.Resources;
using System.Reflection;

namespace LibBot.Services;

public class SharePointService : ISharePointService
{
    private static readonly NLog.Logger _logger;
    private ResourceManager _resourceReader;
    static SharePointService() => _logger = NLog.LogManager.GetCurrentClassLogger();

    private readonly IHttpClientFactory _clientFactory;

    private static List<BookDataResponse> Books { get; set; }
    private static List<string> Filters { get; set; }
    private static DateTime? LastDateUpdate { get; set; }
    public static int AmountBooks { get; } = 8;

    public SharePointService(IHttpClientFactory clientFactory)
    {
        _clientFactory = clientFactory;
        _resourceReader = new ResourceManager("LibBot.Resources.Resource", Assembly.GetExecutingAssembly());
    }

    public async Task<List<BookDataResponse>> GetBooksDataAsync()
    {
        if (Books is null || !LastDateUpdate.HasValue)
        {
            await UpdateBooksDataAsync();
            return Books;
        }

        if (LastDateUpdate.Value.AddHours(2).ToUniversalTime() <= DateTime.UtcNow)
        {
            await UpdateBooksDataAsync();
        }

        return Books;
    }
    public async Task UpdateBooksDataAsync()
    {
        Books = await GetAllBooksFromSharePointAsync();
        UpdateBookPaths(Books);
        LastDateUpdate = DateTime.UtcNow;
    }

    public async Task<bool> IsUserExistInSharePointAsync(string login)
    {
        var client = _clientFactory.CreateClient("SharePoint");

        var httpResponse = await client.GetAsync($"_api/web/siteusers?$filter=Email eq '{login}'&$select=Email");
        if (!httpResponse.IsSuccessStatusCode)
        {
            _logger.Warn(String.Format(_resourceReader.GetString("LogFailedSearchUserByEmail"), httpResponse.StatusCode));
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


    private async Task<List<BookDataResponse>> GetAllBooksFromSharePointAsync()
    {
        var client = _clientFactory.CreateClient("SharePoint");

        var httpResponse = await client.GetAsync($"_api/web/lists/GetByTitle('Books')/items?$select=Title,Id,BookReaderId,TakenToRead,Technology,Created&$top=300&$orderby=Title");

        var contentsString = await httpResponse.Content.ReadAsStringAsync();
        var dataBooks = Book.FromJson(contentsString);

        var result = Book.GetBookDataResponse(dataBooks);

        if (result.Count == 0)
            result = new List<BookDataResponse>();

        return result;
    }


    public async Task<BookChangeStatusResponse> GetDataAboutBookAsync(int bookId)
    {
        var data = new BookChangeStatusResponse();
        var client = _clientFactory.CreateClient("SharePoint");

        var httpResponse = await client.GetAsync($"_api/web/lists/GetByTitle('Books')/items?$select=Id,BookReaderId,TakenToRead,Title&$filter=Id eq {bookId}");

        var contentsString = await httpResponse.Content.ReadAsStringAsync();
        var dataBooks = Book.FromJson(contentsString);

        var result = Book.GetBookDataResponse(dataBooks);

        data.IsBorrowedBook = result[0].BookReaderId is not null;
        data.TakenToRead = result[0].TakenToRead;
        data.Title = result[0].Title;

        return data;
    }
    public async Task<List<BookDataResponse>> GetBooksAsync(int pageNumber)
    {
        var books = await GetBooksDataAsync();
        return books.Skip(pageNumber * AmountBooks).Take(AmountBooks + 1).ToList();
    }


    public async Task<List<BookDataResponse>> GetBooksAsync(int pageNumber, List<string> filters)
    {
        var books = await GetBooksDataAsync();
        var filteredBooks = filters is null ? books : books.Where(book => filters.Any(filter => book.Technology.Results.Any(tech => tech.Label == filter)));
        return filteredBooks.Skip(pageNumber * AmountBooks).Take(AmountBooks + 1).ToList();
    }

    public async Task<List<BookDataResponse>> GetBooksAsync(int pageNumber, string searchQuery)
    {
        var books = await GetBooksDataAsync();
        var filteredBooks = searchQuery is null ? books : books
            .Where(book => book.Title.ToLower().Contains(searchQuery.ToLower()));
        return filteredBooks.Skip(pageNumber * AmountBooks).Take(AmountBooks + 1).ToList();
    }

    public async Task<List<BookDataResponse>> GetBooksAsync(int pageNumber, int? userId)
    {
        var books = await GetBooksDataAsync();
        var filteredBooks = userId is null ? books : books.Where(book => book.BookReaderId.Equals(userId));
        return filteredBooks.Skip(pageNumber * AmountBooks).Take(AmountBooks + 1).ToList();
    }

    public async Task<List<BookDataResponse>> GetNewBooksAsync(int pageNumber) 
    {
        {
            var books = await GetBooksDataAsync();
            var filteredBooks = books.Where(book => book.Created.ToUniversalTime().AddMonths(3) >= DateTime.UtcNow).ToList();
            return filteredBooks.Skip(pageNumber * AmountBooks).Take(AmountBooks + 1).ToList();
        } 
    }

    public void UpdateBookPaths(List<BookDataResponse> books)
    {
        var filters = new List<string>();
        foreach (var book in books)
        {
            foreach(var technology in book.Technology.Results)
            {
                if (!filters.Contains(technology.Label))
                {
                    filters.Add(technology.Label);
                }
            }
        }

        Filters = filters;
    }

    public async Task<string[]> GetBookPathsAsync()
    {
        if(Filters is not null)
        {
            return Filters.ToArray();
        }
        else
        {
            await UpdateBooksDataAsync();
            return Filters.ToArray();
        }
    }
    public async Task<bool> ChangeBookStatusAsync(long chatId, int bookId, ChangeBookStatusRequest bookBorrowRequest)
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
}