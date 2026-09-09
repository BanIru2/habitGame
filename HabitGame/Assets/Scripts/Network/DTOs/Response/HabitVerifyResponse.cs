using Newtonsoft.Json;

/// <summary>
/// 습관 인증 결과를 저장하는 응답 DTO
/// </summary>
public class HabitVerifyResponse
{
    // success, false, retry의 세 가지 결과 정보 (신뢰도가 낮으면 retry 수신)
    [JsonProperty("status")]
    public string Status { get; set; }

    // AI 판정 사유
    [JsonProperty("reason")]
    public string Reason { get; set; }

    // 실제로 인식한 행동/물체 설명
    [JsonProperty("detectedAction")]
    public string DetectedAction { get; set; }

    // 획득할 대상 특성 (Fire, Water, Grass, Aurora)
    [JsonProperty("rewardAttribute")]
    public string RewardAttribute { get; set; }

    // 획득할 특성 경험치
    [JsonProperty("rewardExp")]
    public int RewardExp { get; set; }

    // 보상 수령 후 경험치
    [JsonProperty("totalExp")]
    public int TotalExp { get; set; }

    // 연속 달성 일수
    [JsonProperty("currentStreak")]
    public int CurrentStreak { get; set; }
}
