using Newtonsoft.Json;

namespace LibBot.Models.DbResponse;

public class DbResponse
{
    private string _name;

    [JsonProperty("IdToken")]
    public string IdToken { get; set; }

    [JsonProperty("Id_Token")]
    public string Id_Token
    {
        get { return _name ?? IdToken; }
        set { _name = value; }
    }

    [JsonProperty("RefreshToken")]
    public string RefreshToken { get; set; }

    [JsonProperty("Refresh_Token")]
    public string Refresh_Token
    {
        get { return _name ?? RefreshToken; }
        set { _name = value; }
    }

    [JsonProperty("ExpiresIn")]
    public string ExpiresIn { get; set; }

    [JsonProperty("Expires_In")]
    public string Expires_In
    {
        get { return _name ?? ExpiresIn; }
        set { _name = value; }
    }
}
