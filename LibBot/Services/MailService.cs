using System;
using System.Threading.Tasks;
using LibBot.Models.Configurations;
using LibBot.Services.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace LibBot.Services;

public class MailService : IMailService
{
    private readonly EmailConfiguration _emailConfiguration;
    private readonly BotCredentialsConfiguration _botCredentialsConfiguration;

    public MailService(IOptions<EmailConfiguration> emailConfiguration, IOptions<BotCredentialsConfiguration> botCredentialsConfiguration)
    {
        _emailConfiguration = emailConfiguration.Value;
        _botCredentialsConfiguration = botCredentialsConfiguration.Value;
    }

    public async Task SendAuthenticationCodeAsync(string email, string username, int authenticationCode)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_emailConfiguration.DisplayName, _botCredentialsConfiguration.Login));
        message.To.Add(new MailboxAddress(username, email));
        message.Subject = "LibBot verification Token";

        message.Body = new TextPart("plain")
        {
            Text = $"Dear, {username}"
                   + Environment.NewLine
                   + $"To end registration you can enter the following code into your conversation with bot: {authenticationCode}"
                   + Environment.NewLine
                   + "If you got this email, but Username is not yours, then just ignore it."
        };

        using (var client = new SmtpClient())
        {
            await client.ConnectAsync(_emailConfiguration.Host, _emailConfiguration.Port, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_botCredentialsConfiguration.Login, _botCredentialsConfiguration.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}