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

}