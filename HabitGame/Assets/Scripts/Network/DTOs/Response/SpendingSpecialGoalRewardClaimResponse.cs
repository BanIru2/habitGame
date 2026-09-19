using Newtonsoft.Json;

/// <summary>
/// 소비 목표 완료 및 보상 지급 응답 DTO
/// </summary>
public class SpendingSpecialGoalRewardClaimResponse
{
    [JsonProperty("goalId")]
    public long GoalId { get; set; }

    [JsonProperty("goalName")]
    public string GoalName { get; set; }

    [JsonProperty("rewardGold")]
    public int RewardGold { get; set; }

    [JsonProperty("totalGold")]
    public int TotalGold { get; set; }

    [JsonProperty("isCompleted")]
    public bool IsCompleted { get; set; }

    [JsonProperty("message")]
    public string Message { get; set; }
}
