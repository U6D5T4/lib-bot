using LibBot.Models.SharePointRequests;
using LibBot.Models.SharePointResponses;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LibBot.Services.Interfaces;

public interface ISharePointService
{
    public Task<bool> IsUserExistInSharePointAsync(string login);
    public Task<List<BookDataResponse>> GetBooksFromSharePointAsync(int pageNumber, int? userId = null);
    public int SetNextPageNumberValue(int pageNumber);
    public int SetPreviousPageNumberValue(int pageNumber);
    public Task<string> GetFormDigestValueFromSharePointAsync();
    public Task<UserDataResponse> GetUserDataFromSharePointAsync(string login);
    public Task<bool> ChangeBookStatus(long chatId, int bookId, ChangeBookStatusRequest changeBookStatusRequest);
}