namespace LibBot.Models.SharePointResponses;

public class AuthDbResponse
{
    public string IdToken { get; set; }
    public string RefreshToken { get; set; }
    public string ExpiresIn { get; set; }
}
