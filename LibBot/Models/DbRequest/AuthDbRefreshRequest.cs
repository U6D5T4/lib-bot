namespace LibBot.Models.DbRequest;

public class AuthDbRefreshRequest
{
    public string RefreshToken { get; set; }
    public string GrantType { get; } = "refresh_token";
}
