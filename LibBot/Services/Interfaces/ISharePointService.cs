using LibBot.Models.SharePointRequests;
using LibBot.Models.SharePointResponses;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LibBot.Services.Interfaces;

public interface ISharePointService
{
    Task<string[]> GetBookPathsAsync();
    void UpdateBookPaths(List<BookDataResponse> books);
    Task<bool> IsUserExistInSharePointAsync(string login);
    Task<UserDataResponse> GetUserDataFromSharePointAsync(string login);
    Task<bool> ChangeBookStatusAsync(long chatId, int bookId, ChangeBookStatusRequest changeBookStatusRequest);
    Task<List<BookDataResponse>> GetBooksAsync(int pageNumber);
    Task<List<BookDataResponse>> GetBooksAsync(int pageNumber, List<string> filters);
    Task<List<BookDataResponse>> GetBooksAsync(int pageNumber, string searchQuery);
    Task<List<BookDataResponse>> GetBooksAsync(int pageNumber, int? userId);
    Task UpdateBooksDataAsync();
    Task<List<BookDataResponse>> GetBooksDataAsync();
    Task<BookChangeStatusResponse> GetDataAboutBookAsync(int bookId);
    Task<List<BookDataResponse>> GetNewBooksAsync(int pageNumber);
}