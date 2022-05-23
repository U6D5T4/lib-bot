using System.Threading.Tasks;

namespace LibBot.Services.Interfaces;

public interface IAuthDbService
{
    Task<string> GetAccessToken();
}
