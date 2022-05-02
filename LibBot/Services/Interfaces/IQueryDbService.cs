using System.Threading.Tasks;

namespace LibBot.Services.Interfaces;

public interface IQueryDbService
{
    public Task CreateItemAsync(long chatid, string query);
    public Task UpdateItemAsync(long chatid, string query);
    public Task DeleteItemAsync(long chatid);
    public Task<string> ReadItemAsync(long chatid);

}
