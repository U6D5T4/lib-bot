using System;
using System.Threading.Tasks;
using LibBot.Services.Interfaces;

namespace LibBot.Services
{
    public class UserService : IUserService
    {
        private  const string _domainName = "@itechart - group.com";
        private  readonly  ISharePointService _sharePointService;
        private  readonly  IMailService _mailService;

        public UserService(ISharePointService sharePointService, IMailService mailService)
        {
            _sharePointService = sharePointService;
            _mailService = mailService;
        }

        public bool IsUserExist(long chatId)
        {
            return true;
        }

        public bool WasAuthenticationCodeSendForUser(long chatId)
        {
            return true;
        }

        public bool IsUserVerifyAccount(long chatId)
        {
            return true;
        }

        public Task<bool> IsLoginValid(string login)
        {
            return _sharePointService.IsUserExistInSharePoint(login);
        }

        public int GenerateAuthCodeAndSaveItIntoDatabase()
        {
            throw new System.NotImplementedException();
        }

        public async Task SendEmailWithAuthToken(string login, string username, int authToken)
        {
            var email = login.EndsWith(_domainName) ? login : login + _domainName;
            await _mailService.SendAuthenticationCodeAsync(email, username, authToken);
        }

        public async Task<bool> VerifyAccount(string authCode, long chatId)
        {
            if (int.TryParse(authCode, out var code))
            {
            }

            return false;
        }
    }
}
