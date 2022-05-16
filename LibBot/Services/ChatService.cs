using LibBot.Models;
using LibBot.Services.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LibBot.Services;

public class ChatService : IChatService
{
    private readonly IChatDbService _chatDbService;
    public ChatService(IChatDbService chatDbService)
    {
        _chatDbService = chatDbService;
    }

    public async Task SaveChatInfoAsync(ChatDbModel chatDbModel)
    {
        await _chatDbService.CreateItemAsync(chatDbModel);
    }

    public async Task UpdateChatInfoAsync(ChatDbModel chatDbModel)
    {
       await _chatDbService.UpdateItemAsync(chatDbModel);
    }

    public async Task<ChatDbModel> GetChatInfoAsync(long chatId, int inlineMessageId)
    {
       return await _chatDbService.ReadItemAsync(chatId, inlineMessageId);
    }

    public async Task<IEnumerable<ChatDbModel>> GetUserChatsInfoAsync(long chatId)
    {
        return await _chatDbService.ReadUserItemsAsync(chatId);
    }

    public async Task DeleteChatInfoAsync(long chatId, int inlineMessageId)
    {
         await _chatDbService.DeleteItemAsync(chatId, inlineMessageId);
    }
}
