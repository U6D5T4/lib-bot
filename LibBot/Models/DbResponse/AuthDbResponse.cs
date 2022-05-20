using System;

namespace LibBot.Models.SharePointResponses;

public class AuthDbResponse
{
    public string Token { get; set; }
    public string RefreshToken { get; set; }
    public DateTime ExpiredDate { get; set; }
}
