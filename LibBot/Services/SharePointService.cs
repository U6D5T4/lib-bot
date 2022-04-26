using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
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

    public async Task<List<BookDataResponse>> GetBooksFromSharePointAsync()
    {
        var books = new List<BookDataResponse>();

        try
        {
            var client = _clientFactory.CreateClient("SharePoint");
            var httpResponse = await client.GetAsync($"_api/web/lists/GetByTitle('Books')/items?$select=Title&$skiptoken=Paged=TRUE%26p_ID={PageNumber}&$top={AmountBooks}");
            if (!httpResponse.IsSuccessStatusCode)
            {
                throw new Exception();
            }
            var contentsString = await httpResponse.Content.ReadAsStringAsync();
            var dataBook = Book.FromJson(contentsString);
            books = Book.GetBookDataResponse(dataBook);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

        return books.ToList();
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