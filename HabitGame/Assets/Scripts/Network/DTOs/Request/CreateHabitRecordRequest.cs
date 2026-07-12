using Newtonsoft.Json;

/// <summary>
/// 생활 습관 목표 완료(인증)을 위한 DTO (달성치 Update)
/// </summary>
public class CreateHabitRecordRequest
{
    // 연관된 습관 목표의 고유 ID
    [JsonProperty("goalId")]
    public long GoalId { get; set; }

    [JsonProperty("userId")]
    public long UserId { get; set; }

    // 실제 실천하여 달성한 수치
    [JsonProperty("achievedAmount")]
    public int AchievedAmount { get; set; }

    [JsonProperty("proofImageUrl")]
    public string ProofImageUrl { get; set; }
}
