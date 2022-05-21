using System.Threading.Tasks;

namespace LibBot.Services.Interfaces;

public interface IAuthDbService
{
    Task GetTokens();
    Task UpdateTokens();
    Task<string> GetAccessToken();
}
