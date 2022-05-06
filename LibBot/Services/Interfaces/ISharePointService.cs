using LibBot.Models.SharePointRequests;
using LibBot.Models.SharePointResponses;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LibBot.Services.Interfaces;

public interface ISharePointService
{
    Task<string[]> GetBookPathsAsync();
    Task<bool> IsUserExistInSharePointAsync(string login);
    Task<UserDataResponse> GetUserDataFromSharePointAsync(string login);
    Task<bool> ChangeBookStatus(long chatId, int bookId, ChangeBookStatusRequest changeBookStatusRequest);
    Task<List<BookDataResponse>> GetBooksFromSharePointAsync(int pageNumber);
    Task<List<BookDataResponse>> GetBooksFromSharePointAsync(int pageNumber, int userId);
    Task<List<BookDataResponse>> GetBooksFromSharePointAsync(int pageNumber, string searchQuery);
    Task<List<BookDataResponse>> GetBooksFromSharePointAsync(int pageNumber, List<string> filters);
}