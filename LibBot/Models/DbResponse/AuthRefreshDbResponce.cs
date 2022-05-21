namespace LibBot.Models.DbResponse;

public class AuthRefreshDbResponce
{
    public string Id_Token { get; set; }
    public string Refresh_Token { get; set; }
    public string Expires_In { get; set; }
}
