using Newtonsoft.Json;

/// <summary>
/// 로그인 성공 시 반환되는 응답 DTO
/// </summary>
public class LoginResponse
{
    // API 호출 시 헤더에 포함할 인증용 JWT 토큰
    [JsonProperty("accessToken")]
    public string AccessToken { get; set; }
}