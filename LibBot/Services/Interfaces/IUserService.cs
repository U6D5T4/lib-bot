using LibBot.Models;
using LibBot.Models.SharePointResponses;
using System.Threading.Tasks;

namespace LibBot.Services.Interfaces;

public interface IUserService
{
    public Task<bool> IsUserExistAsync(long chatId);
    public Task<bool> WasAuthenticationCodeSendForUserAsync(long chatId);
    public Task<bool> IsUserVerifyAccountAsync(long chatId);
    public Task<bool> IsLoginValidAsync(string login);
    public Task<int> GenerateAuthCodeAndSaveItIntoDatabaseAsync(long chatId);
    public Task SendEmailWithAuthCodeAsync(long chatId, string username, int authToken);
    public Task<bool> VerifyAccountAsync(string authCode, long chatId);
    public Task CreateUserAsync(long chatId);
    public Task RejectUserAuthCodeAsync(long chatId);
    public Task<bool> IsCodeLifetimeExpiredAsync(long chatId);
    public Task UpdateUserDataAsync(long chatId, UserDataResponse userResponse);
    public string ParseLogin(string login);
    public Task<UserDbModel> GetUserByChatIdAsync(long chatId);
}