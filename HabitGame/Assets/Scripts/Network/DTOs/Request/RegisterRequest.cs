using Newtonsoft.Json;

/// <summary>
/// 자체 계정 회원가입 요청 DTO입니다.
/// </summary>
public class RegisterRequest
{
    [JsonProperty("email")]
    public string Email { get; set; }

    [JsonProperty("password")]
    public string Password { get; set; }

    [JsonProperty("nickname")]
    public string Nickname { get; set; }
}
