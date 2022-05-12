using LibBot.Models;
using LibBot.Models.SharePointResponses;
using System.Threading.Tasks;

namespace LibBot.Services.Interfaces;

public interface IUserService
{
    Task<bool> IsUserExistAsync(long chatId);
    Task<bool> WasAuthenticationCodeSendForUserAsync(long chatId);
    Task<bool> IsUserVerifyAccountAsync(long chatId);
    Task<bool> IsLoginValidAsync(string login);
    Task<int> GenerateAuthCodeAndSaveItIntoDatabaseAsync(long chatId);
    Task SendEmailWithAuthCodeAsync(long chatId, string username, int authToken);
    Task<bool> VerifyAccountAsync(string authCode, long chatId);
    Task CreateUserAsync(long chatId);
    Task RejectUserAuthCodeAsync(long chatId);
    Task<bool> IsCodeLifetimeExpiredAsync(long chatId);
    Task UpdateUserDataAsync(long chatId, UserDataResponse userResponse);
    string ParseLogin(string login);
    Task<UserDbModel> GetUserByChatIdAsync(long chatId);
    Task UpdateUserAsync(UserDbModel user);
    Task SendFeedbackAsync(string feedback);
}