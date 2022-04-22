using LibBot.Models;
using System.Threading.Tasks;

namespace LibBot.Services.Interfaces;
public interface IUserDbService
{
    public Task CreateItemAsync(UserDbModel item);
    public Task<UserDbModel> ReadItemAsync(long chatId);
    public Task UpdateItemAsync(UserDbModel item);
    public Task DeleteItemAsync(long chatId);
}
