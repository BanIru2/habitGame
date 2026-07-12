using Newtonsoft.Json;

/// <summary>
/// 소비 습관 특수 목표의 정보를 담은 응답 DTO
/// </summary>
public class SpendingSpecialGoalResponse
{
    // 특수 목표의 고유 ID
    [JsonProperty("id")]
    public long Id { get; set; }

    [JsonProperty("userId")]
    public long UserId { get; set; }

    [JsonProperty("goalName")]
    public string GoalName { get; set; }

    [JsonProperty("limitAmount")]
    public int LimitAmount { get; set; }

    [JsonProperty("rewardGold")]
    public int RewardGold { get; set; }

    [JsonProperty("isCompleted")]
    public bool IsCompleted { get; set; }

    [JsonProperty("message")]
    public string Message { get; set; }

    // 이 특수 목표가 속한 주간 예산의 ID
    [JsonProperty("budgetId")]
    public long BudgetId { get; set; }

    // 특수 목표의 제목
    [JsonProperty("title")]
    public string Title { get; set; }

    // 특수 목표의 상세 설명
    [JsonProperty("targetDescription")]
    public string TargetDescription { get; set; }

    // 목표 달성 시 지급될 골드
    [JsonProperty("bonusGold")]
    public int BonusGold { get; set; }

    // 달성 여부
    [JsonProperty("isAchieved")]
    public bool IsAchieved { get; set; }

    // 유저가 최종적으로 달성 결과(성공/실패)를 확정했는지 여부
    [JsonProperty("isConfirmed")]
    public bool IsConfirmed { get; set; }

    // 보상 수령 여부
    [JsonProperty("rewardClaimed")]
    public bool RewardClaimed { get; set; }
}
