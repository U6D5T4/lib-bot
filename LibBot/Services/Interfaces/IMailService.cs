using System.Threading.Tasks;

namespace LibBot.Services.Interfaces
{
    public interface IMailService
    {
        public Task SendAuthenticationCodeAsync(string email, string username, int authenticationCode);
    }
}
