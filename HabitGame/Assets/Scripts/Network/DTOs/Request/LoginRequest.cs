using Newtonsoft.Json;

/// <summary>
/// 사용자 로그인을 위한 요청 DTO
/// </summary>
public class LoginRequest
{
    // 사용자 계정 이메일
    [JsonProperty("email")]
    public string Email { get; set; }

    // 사용자 계정 비밀번호
    [JsonProperty("password")]
    public string Password { get; set; }
}