using Newtonsoft.Json;

/// <summary>
/// 소비 습관 보상 수령 응답을 담은 DTO
/// </summary>
public class SpendingRewardClaimResponse
{
    // 예산(Budget)의 고유 ID
    [JsonProperty("budgetId")]
    public long BudgetId { get; set; }

    // 획득 골드량
    [JsonProperty("earnedGold")]
    public int EarnedGold { get; set; }

    // 보상 수령 후의 총 보유 골드
    [JsonProperty("gold")]
    public int Gold { get; set; }

    // 보상 수령 완료 여부
    [JsonProperty("rewardClaimed")]
    public bool RewardClaimed { get; set; }
}
