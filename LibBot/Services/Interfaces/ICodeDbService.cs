using LibBot.Models;
using System.Threading.Tasks;

namespace LibBot.Services.Interfaces;

public interface ICodeDbService
{
    public Task CreateVerifyCodeAsync(ICodeDbModel code);
    public Task<ICodeDbModel> ReadVerifyCodeAsync(int chatId);
    public Task UpdateVerifyCodeAsync(ICodeDbModel code);
    public Task DeleteVerifyCodeAsync(int chatId);
}
