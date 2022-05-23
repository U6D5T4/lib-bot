using LibBot.Models.DbResponse;
using System;

namespace LibBot;

public class Tokens
{
    protected static string Token { get; set; }
    protected static string RefreshToken { get; set; }
    protected static string ExpiresIn { get; set; }
    protected static DateTime CreateTokenDate { get; set; }

    protected void SetDataTokens(DbResponse authDbResponse)
    {
        Token = authDbResponse.IdToken;
        RefreshToken = authDbResponse.RefreshToken;
        ExpiresIn = authDbResponse.ExpiresIn;
        CreateTokenDate = DateTime.Now;
    }
}
