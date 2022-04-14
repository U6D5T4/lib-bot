﻿using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace LibBot.Services.Interfaces;

public interface IHandleUpdateService
{
    public Task SayHelloFromAnton(Update update);
    public Task EchoAsync(Update update);
}
