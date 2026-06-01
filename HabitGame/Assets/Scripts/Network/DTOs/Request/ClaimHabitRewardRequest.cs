using Newtonsoft.Json;

/// <summary>
/// 습관 달성 보상 수령 요청을 위한 DTO
/// </summary>
public class ClaimHabitRewardRequest
{
    // 보상을 수령할 습관 기록의 고유 ID
    [JsonProperty("recordId")]
    public long RecordId { get; set; }
}