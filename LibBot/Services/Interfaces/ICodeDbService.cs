using LibBot.Models;
using System.Threading.Tasks;

namespace LibBot.Services.Interfaces;

public interface ICodeDbService
{
    public Task CreateItemAsync(ICodeDbModel code);
    public Task<ICodeDbModel> ReadItemAsync(int chatId);
    public Task UpdateItemAsync(ICodeDbModel code);
    public Task DeleteItemAsync(int chatId);
}
