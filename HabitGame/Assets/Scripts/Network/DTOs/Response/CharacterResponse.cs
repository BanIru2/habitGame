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

    // ----------------- 아래 필드명 임시. 백엔드 작업 후 통일 필요 -----------------------

    // 불(신체) 속성 경험치 (퍼센트)
    [JsonProperty("fireExpPercentage")]
    public int FireExpPercentage { get; set; }

    // 물(바이오리듬) 속성 경험치 (퍼센트)
    [JsonProperty("waterExpPercentage")]
    public int WaterExpPercentage { get; set; }

    // 풀(환경) 속성 경험치 (퍼센트)
    [JsonProperty("grassExpPercentage")]
    public int GrassExpPercentage { get; set; }

    // 오로라(자기개발) 속성 경험치 (퍼센트)
    [JsonProperty("auroraExpPercentage")]
    public int AuroraExpPercentage { get; set; }

    // 불(신체) 속성 경험치 현재 레벨 요구치
    [JsonProperty("maxFireExp")]
    public int MaxFireExp{ get; set; }

    // 물(바이오리듬) 속성 경험치 현재 레벨 요구치
    [JsonProperty("maxWaterExp")]
    public int MaxWaterExp { get; set; }

    // 풀(환경) 속성 경험치 현재 레벨 요구치
    [JsonProperty("maxGrassExp")]
    public int MaxGrassExp { get; set; }

    // 오로라(자기개발) 속성 경험치 현재 레벨 요구치
    [JsonProperty("maxAuroraExp")]
    public int MaxAuroraExp { get; set; }
}
