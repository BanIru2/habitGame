using Newtonsoft.Json;

/// <summary>
/// 소비 습관 특수 목표의 달성 상태를 업데이트하기 위한 요청 DTO
/// </summary>
public class UpdateSpendingSpecialGoalStatusRequest
{
    // 업데이트할 특수 목표의 고유 ID
    [JsonProperty("specialGoalId")]
    public long SpecialGoalId { get; set; }

    // 목표 달성 여부 (true: 달성, false: 미달성/실패)
    [JsonProperty("isAchieved")]
    public bool IsAchieved { get; set; }
}
