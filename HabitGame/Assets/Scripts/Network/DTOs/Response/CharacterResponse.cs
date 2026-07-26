using Newtonsoft.Json;

/// <summary>
/// 캐릭터의 정보 및 능력치를 담은 응답 DTO
/// </summary>
public class CharacterResponse
{
    // 유저 식별 ID
    [JsonProperty("userId")]
    public long UserId { get; set; }

    // 현재 보유 중인 골드
    [JsonProperty("gold")]
    public int Gold { get; set; }

    // 최대 체력
    [JsonProperty("hp")]
    public float Hp { get; set; }

    // 기본 공격력
    [JsonProperty("atk")]
    public float Atk { get; set; }

    // 기본 방어력
    [JsonProperty("def")]
    public float Def { get; set; }

    // 속도 (턴 우선순위)
    [JsonProperty("spd")]
    public float Spd { get; set; }

    // 치명타 확률
    [JsonProperty("critRate")]
    public float Crit { get; set; }

    // 불(신체) 속성 레벨
    [JsonProperty("fireLv")]
    public int FireLv { get; set; }

    // 물(바이오리듬) 속성 레벨
    [JsonProperty("waterLv")]
    public int WaterLv { get; set; }

    // 풀(환경) 속성 레벨
    [JsonProperty("grassLv")]
    public int GrassLv { get; set; }

    // 오로라(자기개발) 속성 레벨
    [JsonProperty("auroraLv")]
    public int AuroraLv { get; set; }

    // 불(신체) 속성 경험치
    [JsonProperty("fireExp")]
    public int FireExp { get; set; }

    // 물(바이오리듬) 속성 경험치
    [JsonProperty("waterExp")]
    public int WaterExp { get; set; }

    // 풀(환경) 속성 경험치
    [JsonProperty("grassExp")]
    public int GrassExp { get; set; }

    // 오로라(자기개발) 속성 경험치
    [JsonProperty("auroraExp")]
    public int AuroraExp { get; set; }
}
