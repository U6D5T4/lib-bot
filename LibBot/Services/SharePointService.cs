using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using LibBot.Models.SharePointRequests;
using LibBot.Models.SharePointResponses;
using LibBot.Services.Interfaces;
using Newtonsoft.Json;

namespace LibBot.Services;

public class SharePointService : ISharePointService
{
    private readonly IHttpClientFactory _clientFactory;
    private readonly IFileService _fileService;
    
    private static List<BookDataResponse> Books { get; set; }
    private static DateTime? LastDateUpdate { get; set; }

    public async Task<List<BookDataResponse>> GetBooksData()
    {
        if (LastDateUpdate.HasValue)
        {
            if (LastDateUpdate.Value.AddHours(2).ToUniversalTime() >= DateTime.UtcNow)
            {
                if (Books is null)
                {
                    Books = await GetAllBooksFromSharePointAsync();
                    LastDateUpdate = DateTime.UtcNow;
                }
            }
            else
            {
                Books = await GetAllBooksFromSharePointAsync();
                LastDateUpdate = DateTime.UtcNow;
            }
        }
        else
        {
            Books = await GetAllBooksFromSharePointAsync();
            LastDateUpdate = DateTime.UtcNow;
        }

        return Books;
    }
    public async Task UpdateBooksData()
    {
        Books = await GetAllBooksFromSharePointAsync();
        LastDateUpdate= DateTime.UtcNow;
    }

    public static int AmountBooks { get; } = 8;

    public SharePointService(IHttpClientFactory clientFactory, IFileService fileService)
    {
        _clientFactory = clientFactory;
        _fileService = fileService;
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


    private async Task<List<BookDataResponse>> GetAllBooksFromSharePointAsync()
    {
        var client = _clientFactory.CreateClient("SharePoint");

        var httpResponse = await client.GetAsync($"_api/web/lists/GetByTitle('Books')/items?$select=Title,Id,BookReaderId,TakenToRead,Technology&$top=300&$orderby=Title");

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

        var httpResponse = await client.GetAsync($"_api/web/lists/GetByTitle('Books')/items?$select=Id,BookReaderId,TakenToRead&$filter=Id eq {bookId}");

        var contentsString = await httpResponse.Content.ReadAsStringAsync();
        var dataBooks = Book.FromJson(contentsString);

        var result = Book.GetBookDataResponse(dataBooks);

         data.IsBorrowedBook = result[0].BookReaderId is not null;
         data.TakenToRead = result[0].TakenToRead;

         return data;
    }
    public async Task<List<BookDataResponse>> GetBooksAsync(int pageNumber)
    {
        var books = await GetBooksData();
        return books.Skip(pageNumber * AmountBooks).Take(AmountBooks + 1).ToList();
    }


    public async Task<List<BookDataResponse>> GetBooksAsync(int pageNumber, List<string> filters)
    {
        var books = await GetBooksData();
        var filteredBooks = filters is null ? books : books.Where(book => filters.Any(filter => book.Technology.Results.Any(tech => tech.Label == filter)));
        return filteredBooks.Skip(pageNumber * AmountBooks).Take(AmountBooks + 1).ToList();
    }

    public async Task<List<BookDataResponse>> GetBooksAsync(int pageNumber, string searchQuery)
    {
        var books = await GetBooksData();
        var filteredBooks = searchQuery is null ? books : books
            .Where(book => book.Title.ToLower().Contains(searchQuery.ToLower()));
        return filteredBooks.Skip(pageNumber * AmountBooks).Take(AmountBooks + 1).ToList();
    }

    public async Task<List<BookDataResponse>> GetBooksAsync(int pageNumber, int? userId)
    {
        var books = await GetBooksData();
        var filteredBooks = userId is null ? books : books.Where(book => book.BookReaderId.Equals(userId));
        return filteredBooks.Skip(pageNumber * AmountBooks).Take(AmountBooks + 1).ToList();
    }

    public async Task<string[]> GetBookPathsAsync() => await _fileService.GetBookPathsFromFileAsync("bookPaths.txt");

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