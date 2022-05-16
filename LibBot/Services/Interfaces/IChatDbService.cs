using LibBot.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LibBot.Services.Interfaces;

public interface IChatDbService
{
    Task CreateItemAsync(ChatDbModel item);
    Task<ChatDbModel> ReadItemAsync(long chatId);
    Task UpdateItemAsync(ChatDbModel item);
    Task DeleteItemAsync(long chatId);
}
