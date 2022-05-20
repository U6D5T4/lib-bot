namespace LibBot.Models.DbRequest;

public class AuthDbRefreshRequest
{
    public string RefreshToken { get; set; }
    public string Parameter { get; } = "refreshToken";
}
