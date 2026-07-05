using Newtonsoft.Json;

/// <summary>
/// 로그인 성공 시 반환되는 응답 DTO
/// </summary>
public class LoginResponse
{
    [JsonProperty("userId")]
    public long UserId { get; set; }

    [JsonProperty("email")]
    public string Email { get; set; }

    [JsonProperty("nickname")]
    public string Nickname { get; set; }

    [JsonProperty("message")]
    public string Message { get; set; }
}