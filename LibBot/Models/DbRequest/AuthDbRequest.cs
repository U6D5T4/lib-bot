namespace LibBot.Models.SharePointRequests;

public class AuthDbRequest
{
    public string Email { get; set; }
    public string Password { get; set; }
    public bool ReturnSecureToken { get; set; }
}
