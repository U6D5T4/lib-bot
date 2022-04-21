using LibBot.Models;
using System.Threading.Tasks;

namespace LibBot.Services.Interfaces;

public interface ICodeDbService
{
    public Task CreateItemAsync(CodeDbModel item);
    public Task<CodeDbModel> ReadItemAsync(int chatId);
    public Task UpdateItemAsync(CodeDbModel item);
    public Task DeleteItemAsync(int chatId);
}
