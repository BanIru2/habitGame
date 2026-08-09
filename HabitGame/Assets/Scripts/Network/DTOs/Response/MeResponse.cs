using Newtonsoft.Json;

/// <summary>
/// JWT로 인증된 현재 사용자 응답 DTO
/// </summary>
public class MeResponse
{
    [JsonProperty("userId")]
    public long UserId { get; set; }

    [JsonProperty("email")]
    public string Email { get; set; }

    [JsonProperty("nickname")]
    public string Nickname { get; set; }
}
