using Newtonsoft.Json;

/// <summary>
/// 소비 습관 특수 목표 보상 수령 요청을 위한 DTO
/// </summary>
public class SpendingSpecialGoalRewardClaimRequest
{
    // 보상을 수령할 특별 목표의 고유 ID
    [JsonProperty("specialGoalId")]
    public long SpecialGoalId { get; set; }
}
