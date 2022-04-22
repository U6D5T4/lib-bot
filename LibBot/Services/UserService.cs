using System;
using System.Threading.Tasks;
using LibBot.Models;
using LibBot.Models.Configurations;
using LibBot.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace LibBot.Services;

public class UserService : IUserService
{
    private readonly string _domainName;
    private readonly ISharePointService _sharePointService;
    private readonly IMailService _mailService;
    private readonly IUserDbService _userDbService;
    private readonly ICodeDbService _codeDbService;

    public UserService(ISharePointService sharePointService, IMailService mailService, IUserDbService userDbService, ICodeDbService codeDbService, IOptions<SharePointConfiguration> spConfiguration)
    {
        _sharePointService = sharePointService;
        _mailService = mailService;
        _userDbService = userDbService;
        _codeDbService = codeDbService;
        _domainName = spConfiguration.Value.MailDomain;
    }

    public async Task<bool> IsUserExistAsync(long chatId)
    {
        var user = await GetUserByChatIdAsync(chatId);
        return user is not null;
    }

    public async Task<bool> WasAuthenticationCodeSendForUserAsync(long chatId)
    {
        var code = await _codeDbService.ReadItemAsync(chatId);
        return code is not null && code.Code != 0;
    }

    public async Task<bool> IsUserVerifyAccountAsync(long chatId)
    {
        var user = await GetUserByChatIdAsync(chatId);
        return user is not null && user.IsConfirmed;
    }

    public async Task<bool> IsLoginValidAsync(string login)
    {
        var email = ParseLogin(login);
        return await _sharePointService.IsUserExistInSharePointAsync(email);
    }

    public async Task<int> GenerateAuthCodeAndSaveItIntoDatabaseAsync(long chatId)
    {
        var random = new Random();
        var authCode = random.Next(1000, 10_000);

        var code = await GetCodeByChatIdAsync(chatId) ?? new CodeDbModel { ChatId = chatId };
        code.Code = authCode;
        code.ExpiryDate = DateTime.Now.AddMinutes(5);

        await _codeDbService.UpdateItemAsync(code);
        return authCode;
    }

    public async Task SendEmailWithAuthCodeAsync(string login, string username, int authToken)
    {
        try
        {
            var email = ParseLogin(login);
            await _mailService.SendAuthenticationCodeAsync(email, username, authToken);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            throw;
        }
    }

    public async Task<bool> VerifyAccountAsync(string authCode, long chatId)
    {
        var code = await GetCodeByChatIdAsync(chatId);
        if (code is null || code.ExpiryDate < DateTime.Now)
        {
            return false;
        }

        if (int.TryParse(authCode, out var parsedCode))
        {
            var user = await GetUserByChatIdAsync(chatId);
            user.IsConfirmed = code.Code == parsedCode;
            await _userDbService.UpdateItemAsync(user);
            return user.IsConfirmed;
        }

        return false;
    }

    public async Task CreateUserAsync(long chatId)
    {
        var user = new UserDbModel { ChatId = chatId };
        await _userDbService.CreateItemAsync(user);
    }

    public async Task RejectUserAuthCodeAsync(long chatId)
    {
        await _codeDbService.DeleteItemAsync(chatId);
    }

    public async Task<bool> IsCodeLifetimeExpiredAsync(long chatId)
    {
        var code = await GetCodeByChatIdAsync(chatId);
        return code is null || code.ExpiryDate < DateTime.Now;
    }

    private string ParseLogin(string login) => login.EndsWith(_domainName) ? login : login + _domainName;

    private async Task<UserDbModel> GetUserByChatIdAsync(long chatId)
    {
        return await _userDbService.ReadItemAsync(chatId);
    }

    private async Task<CodeDbModel> GetCodeByChatIdAsync(long chatId)
    {
        return await _codeDbService.ReadItemAsync(chatId);
    }
}