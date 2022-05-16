using System.Threading.Tasks;

namespace LibBot.Services.Interfaces;

public interface IMailService
{
    Task SendAuthenticationCodeAsync(string email, string username, int authenticationCode);
}