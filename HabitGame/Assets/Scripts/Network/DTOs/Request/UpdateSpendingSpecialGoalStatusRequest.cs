using Newtonsoft.Json;

/// <summary>
/// 소비 목표 완료 요청 DTO
/// </summary>
public class UpdateSpendingSpecialGoalStatusRequest
{
    [JsonProperty("goalId")]
    public long GoalId { get; set; }
}
