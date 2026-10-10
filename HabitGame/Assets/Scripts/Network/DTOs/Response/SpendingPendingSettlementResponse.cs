
using Newtonsoft.Json;

/// <summary>
/// 지난주 미수령 소비 정산 정보 DTO
/// GET /spending/overview 응답의
/// pendingSettlement 데이터를 저장
/// </summary>
public class SpendingPendingSettlementResponse
{
    // 정산 대상이 된 지난주 예산 ID
    [JsonProperty("budgetId")]
    public long BudgetId { get; set; }

    // 지난주 설정한 예산
    [JsonProperty("budgetAmount")]
    public int BudgetAmount { get; set; }

    // 지난주 최종 소비 금액
    [JsonProperty("currentSpent")]
    public int CurrentSpent { get; set; }

    // 지난주 정산으로 받을 총 Gold
    [JsonProperty("expectedGold")]
    public int ExpectedGold { get; set; }

    // 보상 수령 여부
    [JsonProperty("rewardClaimed")]
    public bool RewardClaimed { get; set; }

    // 주간 소비습관 연속 달성 횟수
    [JsonProperty("streakCount")]
    public int StreakCount { get; set; }

    // 기본 정산 보상
    [JsonProperty("baseReward")]
    public int BaseReward { get; set; }

    // 특수목표 달성 보상
    [JsonProperty("specialGoalReward")]
    public int SpecialGoalReward { get; set; }

    // 연속 달성 추가 보너스
    [JsonProperty("streakBonus")]
    public int StreakBonus { get; set; }
}
