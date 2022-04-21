using System.Threading.Tasks;

namespace LibBot.Services.Interfaces
{
    public interface IUserService
    {
        public bool IsUserExist(long chatId);
        public bool WasAuthenticationCodeSendForUser(long chatId);
        public bool IsUserVerifyAccount(long chatId);
        public Task<bool> IsLoginValid(string login);
        public int GenerateAuthCodeAndSaveItIntoDatabase();
        public Task SendEmailWithAuthToken(string login, string username, int authToken);
        public Task<bool> VerifyAccount(string authCode, long chatId);
    }
}
