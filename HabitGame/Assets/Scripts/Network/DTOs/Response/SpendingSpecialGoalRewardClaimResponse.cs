using Newtonsoft.Json;

/// <summary>
/// 소비 습관 특수 목표 보상 수령 응답을 담은 DTO
/// </summary>
public class SpendingSpecialGoalRewardClaimResponse
{
    // 특별 목표의 고유 ID
    [JsonProperty("specialGoalId")]
    public long SpecialGoalId { get; set; }

    // 연결된 예산(Budget)의 고유 ID
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

    // 갱신된 특별 목표 상세 정보
    [JsonProperty("updatedSpecialGoal")]
    public SpendingSpecialGoalResponse UpdatedSpecialGoal { get; set; }
}
