using LibBot.Models;
using LibBot.Models.SharePointResponses;
using LibBot.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using System.Resources;

namespace LibBot.Services;

public partial class HandleUpdateService : IHandleUpdateService
{
    private static readonly NLog.Logger _logger;
    private ResourceManager _resourceReader;
    static HandleUpdateService() => _logger = NLog.LogManager.GetCurrentClassLogger();

    private readonly IMessageService _messageService;
    private readonly IUserService _userService;
    private readonly ISharePointService _sharePointService;
    private readonly IChatService _chatService;
    private readonly IFeedbackService _feedbackService;

    public HandleUpdateService(IMessageService messageService, IUserService userService, ISharePointService sharePointService, IChatService chatService, IFeedbackService feedbackService)
    {
        _messageService = messageService;
        _userService = userService;
        _sharePointService = sharePointService;
        _chatService = chatService;
        _feedbackService = feedbackService;
        _resourceReader = new ResourceManager("LibBot.Resources.Resource", Assembly.GetExecutingAssembly());
    }

    public async Task HandleAsync(Update update)
    {
        try
        {
            switch (update.Type)
            {
                case UpdateType.Message:
                    if (!await HandleAuthenticationAsync(update.Message))
                    {
                        return;
                    }
                    await BotOnMessageReceived(update.Message!);
                    break;
                case UpdateType.CallbackQuery:
                    await BotOnCallbackQueryReceived(update.CallbackQuery!);
                    break;
                default:
                    break;
            };

        }
        catch (Exception exception)
        {
            await HandleErrorAsync(exception);
        }
    }

    private async Task<bool> HandleAuthenticationAsync(Message message)
    {
        var chatId = message.Chat.Id;
        if (await _userService.IsUserVerifyAccountAsync(chatId))
        {
            return true;
        }

        if (!await _userService.IsUserExistAsync(chatId))
        {
            await _userService.CreateUserAsync(chatId);
            await _messageService.AskToEnterOutlookLoginAsync(message);
            return false;
        }

        if (!await _userService.WasAuthenticationCodeSendForUserAsync(chatId))
        {
            return await GenerateAndSendAuthCodeAsync(message);
        }

        if (await _userService.IsCodeLifetimeExpiredAsync(chatId))
        {
            await CreateAndSendAuthCodeAsync(chatId, message);
            await _messageService.SendTextMessageAndClearKeyboardAsync(chatId, _resourceReader.GetString("ExpiredCode"));
            await _messageService.AskToEnterAuthCodeAsync(message);
            return false;
        }

        if (!await _userService.VerifyAccountAsync(message.Text, chatId))
        {
            await _messageService.SendTextMessageAndClearKeyboardAsync(chatId, _resourceReader.GetString("WrongCode"));
            await _messageService.AskToEnterAuthCodeAsync(message);
            return false;
        }

        return true;
    }

    private async Task<bool> GenerateAndSendAuthCodeAsync(Message message)
    {
        if (await _userService.IsLoginValidAsync(message.Text))
        {
            var login = _userService.ParseLogin(message.Text);
            var userData = await _sharePointService.GetUserDataFromSharePointAsync(login);
            if (userData != null)
            {
                await _userService.UpdateUserDataAsync(message.Chat.Id, userData);
                await CreateAndSendAuthCodeAsync(message.Chat.Id, message);
                await _messageService.AskToEnterAuthCodeAsync(message);
            }
        }
        else
        {
            await _messageService.SendTextMessageAndClearKeyboardAsync(message.Chat.Id, _resourceReader.GetString("WrongCredentials"));
            await _messageService.AskToEnterOutlookLoginAsync(message);
        }

        return false;
    }

    private async Task CreateAndSendAuthCodeAsync(long chatId, Message message)
    {
        try
        {
            var authCode = await _userService.GenerateAuthCodeAndSaveItIntoDatabaseAsync(chatId);
            await _userService.SendEmailWithAuthCodeAsync(chatId, message.Chat.Username, authCode);
        }
        catch
        {
            await _userService.RejectUserAuthCodeAsync(chatId);
            await _messageService.SendTextMessageAndClearKeyboardAsync(chatId,
               _resourceReader.GetString("WrongSendCode"));
            throw;
        }
    }
    
    public Task HandleErrorAsync(Exception exception)
    {
        var ErrorMessage = exception switch
        {
            ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        _logger.Error(exception, ErrorMessage);
        return Task.CompletedTask;
    }

    private string GetFiltersAsAStringMessage(IEnumerable<string> filters) => filters is null ? string.Empty : _resourceReader.GetString("UserFilters") + $"{string.Join(", ", filters)}";

    private async Task<List<BookDataResponse>> GetBookDataResponses(int pageNumber, ChatDbModel data)
    {
        if (data.ChatState == ChatState.NewArrivals)
        {
            return await _sharePointService.GetNewBooksAsync(pageNumber);
        }

        if (data.Filters is not null && data.Filters.Count > 0)
        {
            return await _sharePointService.GetBooksAsync(pageNumber, data.Filters);
        }

        if (!string.IsNullOrWhiteSpace(data.SearchQuery))
        {
            return await _sharePointService.GetBooksAsync(pageNumber, data.SearchQuery);
        }

        return await _sharePointService.GetBooksAsync(pageNumber);
    }
}
