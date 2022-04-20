using LibBot.Models;
using System.Threading.Tasks;

namespace LibBot.Services.Interfaces;
public interface IUserDbService
{
    public Task CreateItemAsync(IUserDbModel user);
    public Task<IUserDbModel> ReadItemAsync(int chatId);
    public Task UpdateItemAsync(IUserDbModel user);
    public Task DeleteItemAsync(int chatId);
}
