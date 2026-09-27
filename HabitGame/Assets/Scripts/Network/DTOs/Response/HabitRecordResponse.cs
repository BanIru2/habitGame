using Newtonsoft.Json;

/// <summary>
/// 습관 실천 기록 정보를 담은 응답 DTO
/// </summary>
public class HabitRecordResponse
{
    // 기록의 고유 ID
    [JsonProperty("id")]
    public long Id { get; set; }

    // 연관된 습관 목표의 ID
    [JsonProperty("goalId")]
    public long GoalId { get; set; }

    // 유저 식별 ID
    [JsonProperty("userId")]
    public long UserId { get; set; }

    // 실제 실천하여 달성한 수치
    [JsonProperty("achievedAmount")]
    public int AchievedAmount { get; set; }

    [JsonProperty("isVerified")]
    public bool IsVerified { get; set; }

    // 기록 처리 후 최신 연속 달성 횟수
    // 백엔드가 아직 해당 필드를 반환하지 않는 경우 null
    [JsonProperty("streakCount")]
    public int? StreakCount { get; set; }

    [JsonProperty("message")]
    public string Message { get; set; }

    // 보상(스탯/골드 등) 수령 완료 여부
    [JsonProperty("rewardClaimed")]
    public bool RewardClaimed { get; set; }

    // 실천 기록 일시
    [JsonProperty("recordedAt")]
    public string RecordedAt { get; set; }
}