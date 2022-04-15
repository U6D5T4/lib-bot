using System;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace LibBot.Services.Interfaces;

public interface IHandleUpdateService
{
    public Task HandleAsync(Update update);
    public Task HandleErrorAsync(Exception exception);
}
