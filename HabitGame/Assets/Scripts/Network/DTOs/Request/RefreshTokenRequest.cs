using Newtonsoft.Json;

public class RefreshTokenRequest
{
    [JsonProperty("refreshToken")]
    public string RefreshToken { get; set; }
}
