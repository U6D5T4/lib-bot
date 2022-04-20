using LibBot.Models;
using System.Threading.Tasks;

namespace LibBot.Services.Interfaces;
public interface IUserDbService
{
    public Task CreateUserAsync(IUserDbModel user);
    public Task<IUserDbModel> ReadUserAsync(int chatId);
    public Task UpdateUserAsync(IUserDbModel user);
    public Task DeleteUserAsync(int chatId);
}
