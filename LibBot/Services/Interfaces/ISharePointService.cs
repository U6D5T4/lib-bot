using LibBot.Models;
using LibBot.Models.SharePointResponses;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LibBot.Services.Interfaces;

public interface ISharePointService
{
    public Task<bool> IsUserExistInSharePointAsync(string login);
    public Task<List<BookDataResponse>> GetBooksFromSharePointAsync();
    public void SetNextPageNumberValue();
    public void SetPreviousPageNumberValue();
    public void SetDefaultPageNumberValue();
    public Task<string> GetFormDigestValueFromSharePointAsync();
    public Task<UserDataResponse> GetUserDataFromSharePointAsync(string login);
    public Task<bool> BorrowBook(long chatId, int bookId, UserDbModel userData);

}