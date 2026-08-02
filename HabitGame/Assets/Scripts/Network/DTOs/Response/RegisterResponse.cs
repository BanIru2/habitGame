using Newtonsoft.Json;

/// <summary>
/// 자체 계정 회원가입 성공 응답 DTO입니다.
/// </summary>
public class RegisterResponse
{
    [JsonProperty("id")]
    public long Id { get; set; }

    [JsonProperty("email")]
    public string Email { get; set; }

    [JsonProperty("nickname")]
    public string Nickname { get; set; }

    [JsonProperty("message")]
    public string Message { get; set; }
}
