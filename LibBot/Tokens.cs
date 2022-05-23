using System;

namespace LibBot;

public class Tokens
{
    protected static string Token { get; set; }
    protected static string RefreshToken { get; set; }
    protected static string ExpiresIn { get; set; }
    protected static DateTime CreateTokenDate { get; set; }
}
